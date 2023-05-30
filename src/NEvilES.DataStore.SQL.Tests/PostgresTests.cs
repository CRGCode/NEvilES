using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using Xunit;

namespace NEvilES.DataStore.SQL.Tests
{
    using NEvilES.Tests.CommonDomain.Sample;
    using Outbox.Abstractions;
    using Xunit.Abstractions;
    using ReadModel = NEvilES.Tests.CommonDomain.Sample.ReadModel;

    [Collection("Serial")]
    public class PostgresTests : IClassFixture<PostgresTestContext>, IDisposable
    {
        private readonly IServiceScope scope;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly Guid chatRoomId;
        private readonly ITestOutputHelper Output;

        public PostgresTests(PostgresTestContext context, ITestOutputHelper output)
        {
            context.OutputHelper = output;
            Output = output;
            serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
            scope = serviceScopeFactory.CreateScope();
            using var s = serviceScopeFactory.CreateScope();
            var commandProcessor = s.ServiceProvider.GetRequiredService<ICommandProcessor>();
            chatRoomId = Guid.NewGuid();
            commandProcessor.Process(new ChatRoom.Create
            {
                ChatRoomId = chatRoomId,
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room",
                State = "VIC"
            });
        }

        [Fact]
        public void WipeAllEvents()
        {
            scope.Dispose();    //  we don't want this as we are going to delete the Db and the context has created a Db transactions
        }

        [Fact]
        public void Read()
        {
            var reader = scope.ServiceProvider.GetRequiredService<IReadEventStore>();
            var events = reader.Read();

            foreach (var e in events)
            {
                Assert.NotEqual(Guid.Empty, e.StreamId);
            }
        }

        [Fact]
        public async Task Save_FirstEvent()
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAsyncRepository>();

            var chatRoom = new ChatRoom.Aggregate();
            chatRoom.RaiseEvent(new ChatRoom.Created
            {
                ChatRoomId = Guid.NewGuid(),
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room"
            });
            var commit = await repository.SaveAsync(chatRoom);

            Assert.NotNull(commit);
        }

        [Fact]
        public void ReplayEvents()
        {
            List<ReadModel.ChatRoom> expected;
            {
                using var serviceScope = serviceScopeFactory.CreateScope();
                expected = serviceScope.ServiceProvider.GetRequiredService<Marten.DocumentRepositoryWithKeyTypeGuid>()
                    .GetAll<ReadModel.ChatRoom>().ToList();
            }
            {
                using var documentRepository = scope.ServiceProvider.GetRequiredService<Marten.DocumentRepositoryWithKeyTypeGuid>();
                documentRepository.WipeDocTypeIfExists<ReadModel.ChatRoom>();
            }
            {
                using var serviceScope = serviceScopeFactory.CreateScope();
                var reader = serviceScope.ServiceProvider.GetRequiredService<IReadEventStore>();
                Pipeline.ReplayEvents.Replay(serviceScope.ServiceProvider.GetRequiredService<IFactory>(), reader);
            }

            List<ReadModel.ChatRoom> actual;
            {
                using var documentRepository = scope.ServiceProvider.GetRequiredService<Marten.DocumentRepositoryWithKeyTypeGuid>();
                actual = documentRepository.GetAll<ReadModel.ChatRoom>().ToList();
            }

            Assert.Equal(expected.First(),actual.First());
        }

        [Fact]
        public void Outbox_Add()
        {
            var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();

            commandProcessor.Process(new ChatRoom.IncludeUserInRoom
            {
                ChatRoomId = chatRoomId,
                UserId = Guid.NewGuid(),
            });


            commandProcessor.Process(new ChatRoom.RenameRoom
            {
                ChatRoomId = chatRoomId,
                NewName = "New ChatRoom"
            });

            var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            repository.Add(new OutboxMessage()
            {
                MessageId = chatRoomId,
                MessageType = "xxxx",
                Destination = "QueueName",
                Payload = "{ IncludeUserInRoom Json }"
            });
        }

        [Fact]
        public async Task Outbox_ProcessMessages()
        {
            var serviceProvider = scope.ServiceProvider;

            var commandProcessor = serviceProvider.GetRequiredService<ICommandProcessor>();
            commandProcessor.Process(new ChatRoom.RenameRoom
            {
                ChatRoomId = chatRoomId,
                NewName = "New ChatRoom"
            });

            var repository = serviceProvider.GetRequiredService<IOutboxRepository>();

            repository.Add(new OutboxMessage()
            {
                MessageId = chatRoomId,
                MessageType = "xxxx",
                Destination = "QueueName",
                Payload = "{ IncludeUserInRoom Json }"
            });

            var outboxWorker = serviceProvider.GetRequiredService<OutboxWorkerWorkerThread>();

            await outboxWorker.StartAsync(new CancellationToken());

            Thread.Sleep(1000);

            outboxWorker.Trigger();

            Thread.Sleep(1000);

            await outboxWorker.StopAsync(new CancellationToken());
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}
