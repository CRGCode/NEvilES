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
        private readonly ITestOutputHelper output;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public SQLEventStoreTests(SQLTestContext context, ITestOutputHelper output)
        {
            this.output = output;
            serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
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

            using var scope = serviceScopeFactory.CreateScope();
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
            {
                using var scope = serviceScopeFactory.CreateScope();
                var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
                commandProcessor.Process(new ChatRoom.Create
                {
                    ChatRoomId = chatRoom,
                    InitialUsers = new HashSet<Guid> { },
                    Name = "Biz Room"
                });
            }
            output.WriteLine($"Chat Room {chatRoom}");
            var done = new List<int>();
            void IncludeUser(int userNumber, Guid guid, Guid userId)
            {
                var retry = 0;
                do
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    try
                    {
                        var commandContext = scope.ServiceProvider.GetRequiredService<ICommandContext>();
                        var factory = scope.ServiceProvider.GetRequiredService<IFactory>();
                        var processor = new CommandProcessor<ChatRoom.IncludeUserInRoom>(factory,commandContext);
                        processor.Process(new ChatRoom.IncludeUserInRoom()
                        {
                            ChatRoomId = guid,
                            UserId = userId
                        });
                        output.WriteLine($"Done User {userNumber} Retry[{retry}]");
                        done.Add(userNumber);

                        return;
                    }
                    catch (AggregateConcurrencyException)
                    {
                        var random = new Random(DateTime.Now.Millisecond);
                        var delay = BackOff[random.Next(retries-1)] + random.Next(10) * (retries - retry);
                        retry++;
                        Thread.Sleep(delay);
                        output.WriteLine($"User {userNumber} Retry[{retry}] - {delay}");
                    }
                } while (retry < retries);
                output.WriteLine($"Exceeded Retries User {userNumber} Retry[{retry}]");
            }

            var tasks = new List<Task>();

            for (var i = 1; i < 10; i++)
            {
                var userNumber = i;
                tasks.Add(Task.Run(() => { IncludeUser(userNumber, chatRoom, Guid.NewGuid()); }));
            }
            Task.WaitAll(tasks.ToArray(), CancellationToken.None);

            output.WriteLine($"All Done {done.Count}!");

            Thread.Sleep(1000);

            {
                using var scope = serviceScopeFactory.CreateScope();
                var reader = scope.ServiceProvider.GetRequiredService<IReadEventStore>();

                var events = reader.Read(chatRoom).ToArray();

                output.WriteLine($"Reader!");

                Assert.Equal(tasks.Count + 1, events.Length);
            }
        }

        [Fact]
        public void ForceConcurrencyExceptions()
        {
            var chatRoom = Guid.NewGuid();

            {
                using var scope = serviceScopeFactory.CreateScope();
                var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
                commandProcessor.Process(new ChatRoom.Create
                {
                    ChatRoomId = chatRoom,
                    InitialUsers = new HashSet<Guid> { },
                    Name = "Biz Room"
                });
            }

            void IncludeUser(Guid guid, Guid userId)
            {
                using var scope = serviceScopeFactory.CreateScope();
                var commandContext = scope.ServiceProvider.GetRequiredService<ICommandContext>();
                var factory = scope.ServiceProvider.GetRequiredService<IFactory>();
                var processor = new CommandProcessor<ChatRoom.IncludeUserInRoom>(factory,commandContext);
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

            output.WriteLine($"ex.InnerExceptions.Count = {ex.InnerExceptions.Count}");
            Assert.All(ex.InnerExceptions, x=> Assert.IsType<AggregateConcurrencyException>(x));
        }

        [Fact]
        public void PipelineProcessorHandlesRetryOnConcurrencyExceptions()
        {
            var chatRoom = Guid.NewGuid();

            {
                using var scope = serviceScopeFactory.CreateScope();
                var commandProcessor = scope.ServiceProvider.GetRequiredService<IPipelineProcessor>();
                commandProcessor.Process(new ChatRoom.Create
                {
                    ChatRoomId = chatRoom,
                    InitialUsers = new HashSet<Guid>(),
                    Name = "Biz Room"
                });
            }

            void IncludeUser(int userNumber, Guid guid, Guid userId)
            {
                using var scope = serviceScopeFactory.CreateScope();
                var commandProcessor = scope.ServiceProvider.GetRequiredService<IPipelineProcessor>();
                commandProcessor.Process(new ChatRoom.IncludeUserInRoom()
                {
                    ChatRoomId = guid,
                    UserId = userId
                });
                output.WriteLine($"User {userNumber}");
            }

            var tasks = new List<Task>();

            for (var i = 0; i < 50; i++)
            {
                var i1 = i;
                tasks.Add(Task.Run(() => { IncludeUser(i1, chatRoom, Guid.NewGuid()); }));
            }

            Task.WaitAll(tasks.ToArray(), CancellationToken.None);

            {
                using var scope = serviceScopeFactory.CreateScope();
                var reader = scope.ServiceProvider.GetRequiredService<IReadEventStore>();

                var events = reader.Read(chatRoom);

                Assert.Equal(tasks.Count + 1, events.Count());
            }
        }

        public void Dispose()
        {
        }
    }
}
