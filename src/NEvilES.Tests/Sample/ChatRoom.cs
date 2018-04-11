using System;
using System.Collections.Generic;

namespace NEvilES.Tests.Sample
{
    public class ChatRoom
    {
        public class Create : ICommand
        {
            public Create()
            {
                InitialUsers = new HashSet<Guid>();
            }

            public Guid StreamId { get; set; }
            public string Name { get; set; }
            public HashSet<Guid> InitialUsers { get; set; }
        }

        public class Created : Create, IEvent
        {

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

        public class UserRemovedFromRoom : RemoveUserFromRoom, IEvent
        {

        }


        public class Aggregate : AggregateBase,
            IHandleAggregateCommand<Create>,
            IHandleAggregateCommand<IncludeUserInRoom>,
            IHandleAggregateCommand<RemoveUserFromRoom>

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



            //------------------------------------------------

            private string Name;
            private HashSet<Guid> UsersInRoom;


            private void Apply(Created e)
            {
                Id = e.StreamId;
                Name = e.Name;
                UsersInRoom = e.InitialUsers;
            }
            private void Apply(UserIncludedInRoom e)
            {
                UsersInRoom.Add(e.UserId);
            }

            private void Apply(UserRemovedFromRoom e)
            {
                UsersInRoom.Remove(e.UserId);
            }
        }

    }
}