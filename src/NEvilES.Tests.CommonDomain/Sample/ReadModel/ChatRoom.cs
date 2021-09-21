using System;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample.ReadModel
{
    public class ChatRoom : IHaveIdentity<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public class Projector :
            IProjectWithResult<Sample.ChatRoom.Created>
        {
            private readonly IReadFromReadModel<Guid> reader;
            private readonly IWriteReadModel<Guid> writer;

            public Projector(IReadFromReadModel<Guid> reader, IWriteReadModel<Guid> writer)
            {
                this.reader = reader;
                this.writer = writer;
            }
            
            public IProjectorResult Project(Sample.ChatRoom.Created message, IProjectorData data)
            {
                var chatRoom = new ChatRoom { Id = message.ChatRoomId };
                writer.Insert(chatRoom);

                return new ProjectorResult(chatRoom);
            }

        }
    }
}