using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.Marten;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;

namespace NEvilES.DataStore.SQL.Tests
{
    public class PostgresTests : IClassFixture<PostgresTestContext>, IDisposable
    {
        private readonly IReadEventStore reader;
        private readonly IServiceScope scope;
        private readonly IFactory factory;
        private readonly IConnectionString connectionString;

        public PostgresTests(PostgresTestContext context)
        {
            connectionString = context.Container.GetRequiredService<IConnectionString>();

            var serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
            scope = serviceScopeFactory.CreateScope();
            factory = scope.ServiceProvider.GetRequiredService<IFactory>();
            reader = scope.ServiceProvider.GetRequiredService<IReadEventStore>();
        }

        [Fact]
        public void WipeAllEvents()
        {
            scope.Dispose();    //  we don't want this as we are going to delete the Db and the context has created a Db transactions
            new PgSQLEventStoreCreate().CreateOrWipeDb(connectionString);
        }

        [Fact]
        public void Read()
        {
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
                StreamId = Guid.NewGuid(),
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room"
            });
            var commit = repository.Save(chatRoom);

            Assert.NotNull(commit);
        }

        [Fact]
        public void ReplayEvents()
        {
            Pipeline.ReplayEvents.Replay(factory,reader);
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}
