using System;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions.Pipeline;
using Xunit;

namespace NEvilES.DataStore.SQL.Tests
{
    public class SQLEventStoreReaderTests : IClassFixture<SQLTestContext>, IDisposable
    {
        private readonly IReadEventStore reader;
        private readonly IServiceScope scope;
        private readonly IFactory factory;

        public SQLEventStoreReaderTests(SQLTestContext context)
        {

            var serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
            scope = serviceScopeFactory.CreateScope();
            factory = scope.ServiceProvider.GetRequiredService<IFactory>();
            reader = scope.ServiceProvider.GetRequiredService<IReadEventStore>();
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
