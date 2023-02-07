using System;
using System.Collections.Generic;
using NEvilES.Abstractions;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class ChatRoom
    {
        public abstract class Id : IMessage
        {
            public Guid GetStreamId() => ChatRoomId;
            public Guid ChatRoomId { get; set; }
        }

        public class Create : Id, ICommand
        {
            public string Name { get; set; }
            public string State { get; set; }

            public HashSet<Guid> InitialUsers { get; set; }

            public Create()
            {
                InitialUsers = new HashSet<Guid>();
            }
        }

        public class Created : Create, IEvent { }

        public class RenameRoom : Id, ICommand
        {
            public string NewName { get; set; }
        }

        public class RoomRenamed : RenameRoom, IEvent { }

        public class IncludeUserInRoom : Id, ICommand
        {
            public Guid UserId { get; set; }
        }

        public class UserIncludedInRoom : IncludeUserInRoom, IEvent { }

        public class RemoveUserFromRoom : Id, ICommand
        {
            public Guid UserId { get; set; }
        }

        public class UserRemovedFromRoom : RemoveUserFromRoom, IEvent { }

        public class Aggregate : AggregateBase,
            IHandleAggregateCommand<Create>,
            IHandleAggregateCommand<IncludeUserInRoom>,
            IHandleAggregateCommand<RemoveUserFromRoom>,
            IHandleAggregateCommand<RenameRoom>
        {
            public void Handle(Create command)
            {
                Raise<Created>(command);
            }

            public void Handle(IncludeUserInRoom command)
            {
                Raise<UserIncludedInRoom>(command);
            }

            public void Handle(RemoveUserFromRoom command)
            {
                Raise<UserRemovedFromRoom>(command);
            }

            public void Handle(RenameRoom command)
            {
                RaiseStateless<RoomRenamed>(command);
            }

            //------------------------------------------------

            private HashSet<Guid> usersInRoom;

            private void Apply(Created e)
            {
                Id = e.ChatRoomId;
                usersInRoom = e.InitialUsers;
            }
            private void Apply(UserIncludedInRoom e)
            {
                usersInRoom.Add(e.UserId);
            }

            private void Apply(UserRemovedFromRoom e)
            {
                usersInRoom.Remove(e.UserId);
            }
        }
    }
}