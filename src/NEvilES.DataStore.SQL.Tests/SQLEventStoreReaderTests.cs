using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Tests.CommonDomain.Sample.ReadModel;
using Xunit;

namespace NEvilES.DataStore.SQL.Tests
{
    //[CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
    [Collection("Serial")]
    public class SQLEventStoreReaderTests : IClassFixture<SQLTestContext>, IDisposable
    {
        private readonly IServiceScope scope;
        private readonly IFactory factory;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public SQLEventStoreReaderTests(SQLTestContext context)
        {
            serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
            scope = serviceScopeFactory.CreateScope();
            factory = scope.ServiceProvider.GetRequiredService<IFactory>();

            {
                var commandProcessor = context.Container.GetRequiredService<ICommandProcessor>();
                commandProcessor.Process(new NEvilES.Tests.CommonDomain.Sample.ChatRoom.Create
                {
                    ChatRoomId = Guid.NewGuid(),
                    InitialUsers = new HashSet<Guid> { },
                    Name = "Biz Room"
                });
            }
        }
    
        [Fact]
        public void Read()
        {
            var reader = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IReadEventStore>();
            var events = reader.Read().ToArray();

            Assert.True(events.Length > 0);
            foreach (var e in events)
            {
                Assert.NotEqual(Guid.Empty, e.StreamId);
            }
        }

        [Fact]
        public void ReplayEvents()
        {
            List<ChatRoom> expected;
            {
                using var serviceScope = serviceScopeFactory.CreateScope();
                expected = serviceScope.ServiceProvider.GetRequiredService<DocumentRepositoryWithKeyTypeGuid>()
                    .GetAll<ChatRoom>().ToList();
            }
            var documentRepository = scope.ServiceProvider.GetRequiredService<DocumentRepositoryWithKeyTypeGuid>();
            documentRepository.WipeDocTypeIfExists<ChatRoom>();
            var reader = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<IReadEventStore>();
            Pipeline.ReplayEvents.Replay(factory,reader);
            var actual = documentRepository.GetAll<ChatRoom>().ToList();
            Assert.Equal(expected.First(),actual.First());
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}
