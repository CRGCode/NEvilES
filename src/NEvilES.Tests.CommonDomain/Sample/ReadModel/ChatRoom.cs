using System;
using System.Collections.Generic;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample.ReadModel
{
    public class ChatRoom : IHaveIdentity<Guid>
    {
        public ChatRoom()
        {
            Users = new List<Guid>();
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public Location Location { get; set; }
        public ChatRoomStatus Status { get; set; }

        public enum ChatRoomStatus
        {
            Active,
            Blocked,
            Closed,
        }

        public class Projector : BaseProjector<ChatRoom>,
            IProjectWithResult<Sample.ChatRoom.Created>,
            IProject<Sample.ChatRoom.UserIncludedInRoom>
        {
            public Projector(IReadFromReadModel<Guid> reader, IWriteReadModel<Guid> writer) : base(reader, writer)
            {
            }

            public IProjectorResult Project(Sample.ChatRoom.Created message, IProjectorData data)
            {
                var chatRoom = new ChatRoom
                    { Id = message.ChatRoomId, Name = message.Name, Location = new Location { State = message.State } };
                Writer.Insert(chatRoom);

                return new ProjectorResult(chatRoom);
            }

            public void Project(Sample.ChatRoom.UserIncludedInRoom message, IProjectorData data)
            {
                var chatRoom = Reader.Get<ReadModel.ChatRoom>(message.ChatRoomId);
                chatRoom.Users.Add(message.UserId);

                Writer.Update(chatRoom);
            }
        }

        public List<Guid> Users { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;
            var other = ((ChatRoom)obj)!;
            return Id == other.Id && Name == other.Name;
        }

        protected bool Equals(ChatRoom other)
        {
            return Id.Equals(other.Id) && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }

    public class Location
    {
        public string State { get; set; }
    }
}