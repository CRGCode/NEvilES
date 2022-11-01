using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.DataStore.SQL.Tests
{

    [Collection("Serial")]
    public class SQLEventStoreTests : IClassFixture<SQLTestContext>, IDisposable
    {
        private readonly SQLTestContext context;
        private readonly ITestOutputHelper testOutputHelper;
        private readonly IConnectionString connectionString;

        public SQLEventStoreTests(SQLTestContext context, ITestOutputHelper testOutputHelper)
        {
            this.context = context;
            this.testOutputHelper = testOutputHelper;
            connectionString = context.Container.GetRequiredService<IConnectionString>();
        }

        [Fact]
        public void Save_Event()
        {
            var chatRoom = new ChatRoom.Aggregate();
            chatRoom.RaiseEvent(new ChatRoom.Created
            {
                ChatRoomId = Guid.NewGuid(),
                InitialUsers = new HashSet<Guid> { }, 
                Name = "Biz Room"
            });

            using var scope = context.Container.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository>();
            var commit = repository.Save(chatRoom);

            Assert.NotNull(commit);
        }

        private static readonly int[] BackOff = {10,20,20,50,50,50,100,100,200,200,300};
        [Fact]
        public void RetryCommandProcessorOnConcurrencyExceptions()
        {
            var retries = BackOff.Count();
            var chatRoom = Guid.NewGuid();
            var commandProcessor = context.Container.GetRequiredService<ICommandProcessor>();
            commandProcessor.Process(new ChatRoom.Create
            {
                ChatRoomId = chatRoom,
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room"
            });

            var done = new List<int>();
            void IncludeUser(int userNumber, Guid guid, Guid userId)
            {
                var retry = 0;
                do
                {
                    var serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
                    using var serviceScope = serviceScopeFactory.CreateScope();
                    try
                    {
                        var commandContext = serviceScope.ServiceProvider.GetRequiredService<ICommandContext>();
                        var processor = new CommandProcessor<ChatRoom.IncludeUserInRoom>(new ScopedServiceProviderFactory(serviceScope),commandContext);
                        processor.Process(new ChatRoom.IncludeUserInRoom()
                        {
                            ChatRoomId = guid,
                            UserId = userId
                        });
                        testOutputHelper.WriteLine($"Done User {userNumber} Retry[{retry}]");
                        done.Add(userNumber);

                        return;
                    }
                    catch (AggregateConcurrencyException)
                    {
                        var random = new Random(DateTime.Now.Millisecond);
                        var delay = BackOff[random.Next(retries-1)] + random.Next(10) * (retries - retry);
                        retry++;
                        Thread.Sleep(delay);
                        testOutputHelper.WriteLine($"User {userNumber} Retry[{retry}] - {delay}");
                    }
                } while (retry < retries);
                testOutputHelper.WriteLine($"Exceeded Retries User {userNumber} Retry[{retry}]");
            }

            var tasks = new List<Task>();

            for (var i = 1; i < 5; i++)
            {
                var userNumber = i;
                tasks.Add(Task.Run(() => { IncludeUser(userNumber, chatRoom, Guid.NewGuid()); }));
            }
            Task.WaitAll(tasks.ToArray(), CancellationToken.None);

            testOutputHelper.WriteLine($"All Done {done.Count}!");

            Thread.Sleep(1000);

            var reader = context.Container.GetRequiredService<IReadEventStore>();

            var events = reader.Read(chatRoom).ToArray();
            
            testOutputHelper.WriteLine($"Reader!");

            Assert.Equal(tasks.Count + 1, events.Length);
        }

        [Fact]
        public void ForceConcurrencyExceptions()
        {
            var chatRoom = Guid.NewGuid();
            var commandProcessor  = context.Container.GetRequiredService<ICommandProcessor>();
            commandProcessor.Process(new ChatRoom.Create
            {
                ChatRoomId = chatRoom,
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room"
            });

            
            void IncludeUser(Guid guid, Guid userId)
            {
                using var scope = context.Container.CreateScope();
                var commandContext = scope.ServiceProvider.GetRequiredService<ICommandContext>();
                var processor = new CommandProcessor<ChatRoom.IncludeUserInRoom>(new ScopedServiceProviderFactory(scope), commandContext);
                processor.Process(new ChatRoom.IncludeUserInRoom()
                {
                    ChatRoomId = guid,
                    UserId = userId
                });
            }

            var tasks = new List<Task>();

            var ex = Assert.Throws<AggregateException>(() =>
            {
                for (var i = 0; i < 20; i++)
                {
                    tasks.Add(Task.Run(() => { IncludeUser(chatRoom, Guid.NewGuid()); }));
                }
                Task.WaitAll(tasks.ToArray(), CancellationToken.None);
            });

            testOutputHelper.WriteLine($"ex.InnerExceptions.Count = {ex.InnerExceptions.Count}");
            Assert.All(ex.InnerExceptions, x=> Assert.IsType<AggregateConcurrencyException>(x));
        }

        [Fact]
        public void PipelineProcessorHandlesRetryOnConcurrencyExceptions()
        {
            var chatRoom = Guid.NewGuid();
            var commandProcessor = context.Container.GetRequiredService<ICommandProcessor>();
            commandProcessor.Process(new ChatRoom.Create
            {
                ChatRoomId = chatRoom,
                InitialUsers = new HashSet<Guid>(),
                Name = "Biz Room"
            });

            void IncludeUser(int userNumber, Guid guid, Guid userId)
            {
                commandProcessor.Process(new ChatRoom.IncludeUserInRoom()
                {
                    ChatRoomId = guid,
                    UserId = userId
                });
                testOutputHelper.WriteLine($"User {userNumber}");
            }

            var tasks = new[]
            {
                new Task(() => { IncludeUser(1, chatRoom, Guid.NewGuid()); }),
                new Task(() => { IncludeUser(2, chatRoom, Guid.NewGuid()); }),
                new Task(() => { IncludeUser(3, chatRoom, Guid.NewGuid()); }),
                new Task(() => { IncludeUser(4, chatRoom, Guid.NewGuid()); }),
                new Task(() => { IncludeUser(5, chatRoom, Guid.NewGuid()); }),
                new Task(() => { IncludeUser(6, chatRoom, Guid.NewGuid()); }),
                new Task(() => { IncludeUser(7, chatRoom, Guid.NewGuid()); }),
                new Task(() => { IncludeUser(8, chatRoom, Guid.NewGuid()); })
            };

            foreach (var task in tasks)
            {
                task.Start();
            }

            Task.WaitAll(tasks, CancellationToken.None);

            var reader = context.Container.GetRequiredService<IReadEventStore>();

            var events = reader.Read(chatRoom);

            Assert.Equal(tasks.Length + 1, events.Count());
        }

        public void Dispose()
        {
        }
    }
}
