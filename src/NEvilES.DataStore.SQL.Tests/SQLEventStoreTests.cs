using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marten.Events.Projections.Async.ErrorHandling;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.MSSQL;
using NEvilES.Pipeline;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.DataStore.SQL.Tests
{
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
        public void WipeAllEvents()
        {
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

            using var scope = context.Container.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository>();
            var commit = repository.Save(chatRoom);

            Assert.NotNull(commit);
        }

        [Fact]
        public void ForceConcurrencyExceptions()
        {
            var chatRoom = Guid.NewGuid();
            var commandProcessor  = context.Container.GetRequiredService<ICommandProcessor>();
            commandProcessor.Process(new ChatRoom.Create
            {
                StreamId = chatRoom,
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room"
            });

            
            void IncludeUser(Guid guid, Guid userId)
            {
                using var scope = context.Container.CreateScope();
                //var processor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
                var commandContext = scope.ServiceProvider.GetRequiredService<ICommandContext>();
                var processor = new CommandProcessor<ChatRoom.IncludeUserInRoom>(new ScopedServiceProviderFactory(scope), commandContext);
                processor.Process(new ChatRoom.IncludeUserInRoom()
                {
                    StreamId = guid,
                    UserId = userId
                });
            }

            var tasks = new[]
            {
                new Task(() => { IncludeUser(chatRoom, Guid.NewGuid()); }),
                new Task(() => { IncludeUser(chatRoom, Guid.NewGuid()); }),
                new Task(() => { IncludeUser(chatRoom, Guid.NewGuid()); }),
                new Task(() => { IncludeUser(chatRoom, Guid.NewGuid()); })
            };

            var ex = Assert.Throws<AggregateException>(() =>
            {
                foreach (var task in tasks)
                {
                    task.Start();
                }

                Task.WaitAll(tasks, CancellationToken.None);
            });

            Assert.Equal(3,ex.InnerExceptions.Count);
            Assert.All(ex.InnerExceptions, x=> Assert.IsType<AggregateConcurrencyException>(x));
        }

        const int RETRIES = 10;
        private static readonly int[] BackOff = {10,20,50,100,200,300,500,600,700,1000};

        [Fact]
        public void RetryCommandProcessorOnConcurrencyExceptions()
        {
            var chatRoom = Guid.NewGuid();
            var commandProcessor = context.Container.GetRequiredService<ICommandProcessor>();
            commandProcessor.Process(new ChatRoom.Create
            {
                StreamId = chatRoom,
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room"
            });

            void IncludeUser(int userNumber, Guid guid, Guid userId)
            {
                var retry = 0;
                do
                {
                    var serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
                    using (var serviceScope = serviceScopeFactory.CreateScope())
                    {
                        try
                        {
                            var commandContext = serviceScope.ServiceProvider.GetRequiredService<ICommandContext>();
                            var processor = new CommandProcessor<ChatRoom.IncludeUserInRoom>(new ScopedServiceProviderFactory(serviceScope),commandContext);
                            processor.Process(new ChatRoom.IncludeUserInRoom()
                            {
                                StreamId = guid,
                                UserId = userId
                            });
                            return;

                        }
                        catch (AggregateConcurrencyException)
                        {
                            var delay = BackOff[retry++];
                            Thread.Sleep(delay);
                            testOutputHelper.WriteLine($"User {userNumber} Retry[{retry}] - {delay}");
                        }
                    }
                    
                } while (retry < RETRIES);
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

        [Fact]
        public void PipelineProcessorHandlesRetryOnConcurrencyExceptions()
        {
            var chatRoom = Guid.NewGuid();
            var commandProcessor = context.Container.GetRequiredService<ICommandProcessor>();
            commandProcessor.Process(new ChatRoom.Create
            {
                StreamId = chatRoom,
                InitialUsers = new HashSet<Guid>(),
                Name = "Biz Room"
            });

            void IncludeUser(int userNumber, Guid guid, Guid userId)
            {
                commandProcessor.Process(new ChatRoom.IncludeUserInRoom()
                {
                    StreamId = guid,
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
