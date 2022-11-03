using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

using Xunit;

namespace NEvilES.DataStore.SQL.Tests
{
    using NEvilES.Tests.CommonDomain.Sample;
    using ReadModel = NEvilES.Tests.CommonDomain.Sample.ReadModel;

    [Collection("Serial")]
    public class PostgresTests : IClassFixture<PostgresTestContext>, IDisposable
    {
        private readonly IServiceScope scope;
        private readonly IFactory factory;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public PostgresTests(PostgresTestContext context)
        {
            serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
            scope = serviceScopeFactory.CreateScope();
            factory = scope.ServiceProvider.GetRequiredService<IFactory>();
            {
                using var s = serviceScopeFactory.CreateScope();
                var commandProcessor = s.ServiceProvider.GetRequiredService<ICommandProcessor>();
                commandProcessor.Process(new ChatRoom.Create
                {
                    ChatRoomId = Guid.NewGuid(),
                    InitialUsers = new HashSet<Guid> { },
                    Name = "Biz Room"
                });
            }
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
        public void Save_FirstEvent()
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRepository>();

            var chatRoom = new ChatRoom.Aggregate();
            chatRoom.RaiseEvent(new ChatRoom.Created
            {
                ChatRoomId = Guid.NewGuid(),
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room"
            });
            var commit = repository.Save(chatRoom);

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
            var documentRepository = scope.ServiceProvider.GetRequiredService<Marten.DocumentRepositoryWithKeyTypeGuid>();
            documentRepository.WipeDocTypeIfExists<ReadModel.ChatRoom>();
            var reader = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IReadEventStore>();
            Pipeline.ReplayEvents.Replay(factory,reader);
            var actual = documentRepository.GetAll<ReadModel.ChatRoom>().ToList();
            Assert.Equal(expected.First(),actual.First());
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}
