using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Tests.CommonDomain.Sample;
using Npgsql;
using Outbox.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.DataStore.SQL.Tests
{
    public class PostgresOutBoxTests : IClassFixture<PostgresTestContext>, IDisposable
    {
        private readonly IServiceScope scope;

        public PostgresOutBoxTests(PostgresTestContext context, ITestOutputHelper output)
        {
            context.OutputHelper = output;
            var serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
            scope = serviceScopeFactory.CreateScope();
        }

        [Fact]
        public void Basic_test()
        {
            var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
            var chatRoomId = Guid.NewGuid();

            commandProcessor.Process(new ChatRoom.Create
            {
                ChatRoomId = chatRoomId,
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room",
                State = "VIC"
            });
        }

        [Fact]
        public void Outbox_Add()
        {
            var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
            var chatRoomId = Guid.NewGuid();

            commandProcessor.Process(new ChatRoom.Create
            {
                ChatRoomId = chatRoomId,
                InitialUsers = new HashSet<Guid> { },
                Name = "Biz Room",
                State = "VIC"
            });

            commandProcessor.Process(new ChatRoom.IncludeUserInRoom
            {
                ChatRoomId = chatRoomId,
                UserId = Guid.NewGuid(),
            });

            var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            repository.Add(new OutboxMessage()
            {
                MessageId = chatRoomId,
                MessageType = "xxxx",
                Destination = "QueueName",
                Payload = "{ IncludeUserInRoom Json }"
            });
        }

        [Fact]
        public void Outbox_Remove()
        {
            var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();

            var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            repository.Add(new OutboxMessage()
            {
                MessageId = Guid.NewGuid(),
                MessageType = $"{typeof(ChatRoom)}",
                Destination = "QueueName",
                Payload = "{ ChatRoom Json }"
            });
        }

        [Fact]
        public void Outbox_ProcessMessages()
        {
            var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();

            var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            foreach (var outboxMessage in repository.GetNext())
            {
                repository.Remove(outboxMessage.Id);
            }
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }



 /*
    public class EFOutboxRepository : DbContext, IOutboxRepository
    {

        public EFOutboxRepository(DbContextOptions<EFOutboxRepository> options)
            : base(options)
        {
            
        }
        public virtual DbSet<OutboxMessage> Outbox { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>();
        }

        public void Add(OutboxMessage message)
        {
            Outbox.Add(message);
            SaveChanges();
        }

        public IEnumerable<OutboxMessage> GetNext()
        {
            return Outbox;
        }

        public void Remove(int messageId)
        {
            var message = Outbox.First(x => x.Id == messageId);
            Outbox.Remove(message);
            SaveChanges();
        }
    }
 */
}