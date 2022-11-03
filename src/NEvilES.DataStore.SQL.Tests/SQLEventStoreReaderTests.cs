using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Tests.CommonDomain.Sample.ReadModel;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.DataStore.SQL.Tests
{
    //[CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
    [Collection("Serial")]
    public class SQLEventStoreReaderTests : IClassFixture<SQLTestContext>, IDisposable
    {
        private readonly ITestOutputHelper output;
        private readonly IServiceScope scope;
        private readonly IFactory factory;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public SQLEventStoreReaderTests(SQLTestContext context, ITestOutputHelper output)
        {
            this.output = output;
            serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
            scope = serviceScopeFactory.CreateScope();
            factory = scope.ServiceProvider.GetRequiredService<IFactory>();

            {
                using var s = serviceScopeFactory.CreateScope();

                var commandProcessor = s.ServiceProvider.GetRequiredService<ICommandProcessor>();
                var chatRoomId = Guid.NewGuid();
                commandProcessor.Process(new NEvilES.Tests.CommonDomain.Sample.ChatRoom.Create
                {
                    ChatRoomId = chatRoomId,
                    InitialUsers = new HashSet<Guid> { },
                    Name = "Biz Room"
                });
                output.WriteLine($"{chatRoomId}");
            }
        }
    
        [Fact]
        public void Read()
        {
            var reader = scope.ServiceProvider.GetRequiredService<IReadEventStore>();
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
