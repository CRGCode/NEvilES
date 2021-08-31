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
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.DataStore.SQL.Tests
{
    public class SQLEventStoreTests : IDisposable, IClassFixture<SQLTestContext>
    {
        private readonly SQLTestContext context;
        private readonly ITestOutputHelper testOutputHelper;
        private readonly IRepository repository;
        private readonly IConnectionString connectionString;
        private readonly IServiceScope scope;

        public SQLEventStoreTests(SQLTestContext context, ITestOutputHelper testOutputHelper)
        {
            this.context = context;
            this.testOutputHelper = testOutputHelper;
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

        public void RunCommand<TCommand>(TCommand command) where TCommand : ICommand
        {
            var serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
            using (var serviceScope = serviceScopeFactory.CreateScope())
            {
                var processor = serviceScope.ServiceProvider.GetRequiredService<ICommandProcessor>();
                processor.Process(command);
            }
        }

        [Fact]
        public void ForceConcurrencyExceptions()
        {
            var chatRoom = Guid.NewGuid();
            RunCommand(new ChatRoom.Create
            {
                StreamId = chatRoom,
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room"
            });

            void IncludeUser(Guid guid, Guid userId)
            {
                RunCommand(new ChatRoom.IncludeUserInRoom()
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
        public void RetryOnConcurrencyExceptions()
        {
            void IncludeUser(int userNumber, Guid guid, Guid userId)
            {
                var retry = 0;
                do
                {
                    try
                    {
                        var serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
                        using (var serviceScope = serviceScopeFactory.CreateScope())
                        {
                            var processor = serviceScope.ServiceProvider.GetRequiredService<ICommandProcessor>();
                            processor.Process(new ChatRoom.IncludeUserInRoom()
                            {
                                StreamId = guid,
                                UserId = userId
                            });
                        }

                        return;
                    }
                    catch (AggregateConcurrencyException)
                    {
                        var delay = BackOff[retry++];
                        Thread.Sleep(delay);
                        testOutputHelper.WriteLine($"User {userNumber} Retry[{retry}] - {delay}");
                    }
                } while (retry < RETRIES);

                RunCommand(new ChatRoom.IncludeUserInRoom()
                {
                    StreamId = guid,
                    UserId = userId
                });
            }

            var chatRoom = Guid.NewGuid();
            RunCommand(new ChatRoom.Create
            {
                StreamId = chatRoom,
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room"
            });

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

            var reader = scope.ServiceProvider.GetRequiredService<IReadEventStore>();

            var events = reader.Read(chatRoom);

            Assert.Equal(tasks.Length + 1, events.Count());
        }


        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}
