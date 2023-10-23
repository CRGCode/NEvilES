using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();

            commandProcessor.Process(new ChatRoom.IncludeUserInRoom
            {
                ChatRoomId = chatRoomId,
                UserId = Guid.NewGuid(),
            });

            List<ReadModel.ChatRoom> expected;
            {
                using var serviceScope = serviceScopeFactory.CreateScope();
                expected = serviceScope.ServiceProvider.GetRequiredService<Marten.DocumentRepositoryWithKeyTypeGuid>()
                    .GetAll<ReadModel.ChatRoom>().ToList();
            }
            Output.WriteLine($"Expected count {expected.Count}");
            {
                using var serviceScope = serviceScopeFactory.CreateScope();
                var documentRepository = serviceScope.ServiceProvider.GetRequiredService<Marten.DocumentRepositoryWithKeyTypeGuid>();
                documentRepository.WipeDocTypeIfExists<ReadModel.ChatRoom>();
            }

            {
                using var serviceScope = serviceScopeFactory.CreateScope();
                var wiped = serviceScope.ServiceProvider.GetRequiredService<Marten.DocumentRepositoryWithKeyTypeGuid>()
                    .GetAll<ReadModel.ChatRoom>().ToList();
                Output.WriteLine($"Wiped count {wiped.Count}");
            }

            {
                using var serviceScope = serviceScopeFactory.CreateScope();
                var reader = serviceScope.ServiceProvider.GetRequiredService<IReadEventStore>();
                var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<PostgresTests>>();
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
        public void Process()
        {
            var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();

            commandProcessor.Process(new ChatRoom.IncludeUserInRoom
            {
                ChatRoomId = chatRoomId,
                UserId = Guid.NewGuid(),
            });


            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<Guid>>();

            var chatRoom = reader.Get<ReadModel.ChatRoom>(chatRoomId);

            Output.WriteLine($"chatRoom.Users.Count = {chatRoom.Users.Count}");
            Assert.Single(chatRoom.Users);
        }

        [Fact(Skip = "Postgres issues")]
        public async Task ProcessAsync()
        {
            var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();

            await commandProcessor.ProcessAsync(new ChatRoom.IncludeUserInRoom
            {
                ChatRoomId = chatRoomId,
                UserId = Guid.NewGuid(),
            });


            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<Guid>>();

            var chatRoom = reader.Get<ReadModel.ChatRoom>(chatRoomId);

            Assert.True(chatRoom.Users.Count > 0);
        }

        [Fact]
        public void CommandRaises2Events()
        {
            var streamId = Guid.NewGuid();
            var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<Guid>>();
            commandProcessor.Process(new Customer.Create() { CustomerId = streamId, Details = new PersonalDetails("John", "Smith") });

            const string reason = "Some reason for complaining";
            commandProcessor.Process(new Customer.Complain { CustomerId = streamId, Reason = reason });

            var customer = reader.Get<ReadModel.Customer>(streamId);
            Assert.Equal(reason, customer.Complaints.First());
            Assert.Equal(reason, customer.Notes.First());
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

            commandProcessor.Process(new ChatRoom.RenameRoom
            {
                ChatRoomId = chatRoomId,
                NewName = "New ChatRoom2"
            });

            var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            repository.Add(new OutboxMessage()
            {
                MessageId = chatRoomId,
                MessageType = "xxxx",
                Destination = "QueueName",
                Payload = "{ IncludeUserInRoom Json }"
            });
            var outboxMessages = repository.GetNext().ToArray();
            Assert.Equal(3, outboxMessages.Length);
        }

        [Fact]
        public async Task Outbox_ProcessMessages()
        {
            var serviceProvider = scope.ServiceProvider;

            var tran = serviceProvider.GetRequiredService<IDbTransaction>();
            var repository = serviceProvider.GetRequiredService<IOutboxRepository>();
            repository.Add(new OutboxMessage()
            {
                MessageId = chatRoomId,
                MessageType = "xxxx",
                Destination = "QueueName",
                Payload = "{ IncludeUserInRoom Json }"
            });
            tran.Commit();

            var commandProcessor = serviceProvider.GetRequiredService<ICommandProcessor>();
            commandProcessor.Process(new ChatRoom.RenameRoom
            {
                ChatRoomId = chatRoomId,
                NewName = "New ChatRoom"
            });
            
            var outboxMessages = repository.GetNext().ToArray();

            Output.WriteLine($"Outbox count {outboxMessages.Length}");
            Assert.True(outboxMessages.Length >= 2);

            var hostWorker = serviceProvider.GetRequiredService<OutboxWorkerSendingMessages>();

            var cts = new CancellationTokenSource();
            await hostWorker.StartAsync(cts.Token);

            Thread.Sleep(10);

            var outboxWorker = (ITriggerOutbox)hostWorker;
            outboxWorker.Trigger();

            Thread.Sleep(10);

            outboxMessages = repository.GetNext().ToArray();
            Output.WriteLine($"Outbox count {outboxMessages.Length}");
            Assert.Empty(outboxMessages);

            cts.Cancel();
            await hostWorker.StopAsync(cts.Token);
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}
