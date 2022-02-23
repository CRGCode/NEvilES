using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;
using Xunit;

namespace NEvilES.Tests
{
    using CommonDomain.Sample.ReadModel;

    public class ReadModelTests : IClassFixture<SharedFixtureContext>, IDisposable
    {
        private readonly IServiceScope scope;

        public ReadModelTests(SharedFixtureContext context)
        {
            scope = context.Container.CreateScope();
        }

        [Fact]
        public void ReaderForGuid_Null()
        {
            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<Guid>>();

            var chatRoom = reader.Get<ChatRoom>(Guid.NewGuid());
            Assert.Null(chatRoom);
        }

        [Fact]
        public void ReaderForString_Null()
        {
            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<string>>();

            var dashboard = reader.Get<Dashboard>("12345");

            Assert.Null(dashboard);
        }

        [Fact]
        public void WriterForString()
        {
            var writer = scope.ServiceProvider.GetRequiredService<IWriteReadModel<string>>();

            writer.Insert(new Dashboard{ Id = "1234" });

            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<string>>();

            var dashboard =  reader.Get<Dashboard>("1234");

            Assert.Equal("1234", dashboard.Id);
        }

        [Fact]
        public void WriterForGuid()
        {
            var writer = scope.ServiceProvider.GetRequiredService<IWriteReadModel<Guid>>();

            var id = Guid.NewGuid();
            writer.Insert(new ChatRoom { Id = id });

            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<Guid>>();

            var person = reader.Get<ChatRoom>(id);

            Assert.Equal(id, person.Id);
        }

        [Fact]
        public void ProjectorWorks()
        {
            var projector = scope.ServiceProvider.GetRequiredService<IProjectWithResult<CommonDomain.Sample.ChatRoom.Created>>();
            Assert.NotNull(projector);

            var processor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();

            var results =  processor.Process(new CommonDomain.Sample.ChatRoom.Create { ChatRoomId = Guid.NewGuid(), Name = "ChatRoom 1", InitialUsers = new HashSet<Guid>() });

            Assert.Single(results.ReadModelItems);
        }


        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}