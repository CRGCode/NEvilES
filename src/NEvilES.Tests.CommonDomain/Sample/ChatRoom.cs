using System;
using System.Collections.Generic;
using NEvilES.Abstractions;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class ChatRoom
    {
        public class Create : Created, ICommand
        {
            public Create()
            {
                InitialUsers = new HashSet<Guid>();
            }

        }

        public class Created : Event
        {
            public string Name { get; set; }
            public HashSet<Guid> InitialUsers { get; set; }
        }

        public class RenameRoom : RoomRenamed, ICommand { }
        public class RoomRenamed : Event
        {
            public string NewName { get; set; }
        }

        public class IncludeUserInRoom : ICommand
        {
            public Guid StreamId { get; set; }
            public Guid UserId { get; set; }
        }

        public class UserIncludedInRoom : IncludeUserInRoom, IEvent
        {

        }

        public class RemoveUserFromRoom : ICommand
        {
            public Guid StreamId { get; set; }
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
                Id = e.StreamId;
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