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
                DefaultUsers = new HashSet<Guid>();
            }

            public Guid StreamId { get; set; }
            public string Name { get; set; }
            public HashSet<Guid> DefaultUsers { get; set; }
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
                throw new NotImplementedException();
            }

            public void Handle(IncludeUserInRoom command)
            {
                throw new NotImplementedException();
            }

            public void Handle(RemoveUserFromRoom command)
            {
                throw new NotImplementedException();
            }



            //------------------------------------------------

            private string Name;
            private HashSet<Guid> UsersInRoom;


            private void Apply(Created e)
            {
                Name = e.Name;
                UsersInRoom = e.DefaultUsers;
            }
            private void Apply(IncludeUserInRoom e)
            {
                UsersInRoom.Add(e.UserId);
            }

            private void Apply(RemoveUserFromRoom e)
            {
                UsersInRoom.Remove(e.UserId);
            }
        }

    }
}