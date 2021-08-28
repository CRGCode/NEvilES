using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.DataStore.MSSQL;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;

namespace NEvilES.DataStore.SQL.Tests
{
    public class SQLEventStoreTests : IDisposable, IClassFixture<SQLTestContext>
    {
        private readonly IRepository repository;
        private readonly IConnectionString connectionString;
        private readonly IServiceScope scope;

        public SQLEventStoreTests(SQLTestContext context)
        {
            connectionString = context.Container.GetRequiredService<IConnectionString>();

            var serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
            scope = serviceScopeFactory.CreateScope();
            repository = scope.ServiceProvider.GetRequiredService<IRepository>();
        }

        [Fact]
        public void WipeAllEvents()
        {
            scope.Dispose();    //  we don't want this as we are going to delete the Db and the context has created a Db transactions
            new MSSQLEventStoreCreate().CreateOrWipeDb(connectionString);
        }

        [Fact]
        public void Save_FirstEvent()
        {
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

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}
