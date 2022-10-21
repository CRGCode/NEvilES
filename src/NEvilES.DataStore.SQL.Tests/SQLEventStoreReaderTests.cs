using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Tests.CommonDomain.Sample.ReadModel;
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
            // reader must be in separate DB scope
            reader = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IReadEventStore>();
        }

        [Fact]
        public void Read()
        {
            var events = reader.Read().ToArray();

            Assert.True(events.Length > 2);
            foreach (var e in events)
            {
                Assert.NotEqual(Guid.Empty, e.StreamId);
            }
        }

        [Fact]
        public void ReplayEvents()
        {
            var documentRepository = scope.ServiceProvider.GetRequiredService<DocumentRepositoryWithKeyTypeGuid>();
            var expected = documentRepository.GetAll<ChatRoom>();
            documentRepository.WipeDocTypeIfExists<ChatRoom>();
            Assert.Throws<Exception>(() =>
            {
                var x= documentRepository.GetAll<ChatRoom>();
            });
            Pipeline.ReplayEvents.Replay(factory,reader);
            var actual = documentRepository.GetAll<ChatRoom>();
            Assert.Equal(expected,actual);
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}
