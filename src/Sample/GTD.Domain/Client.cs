using System;
using System.Collections.Generic;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace GTD.Domain
{
    public abstract class Client
    {
        public abstract class Id : IMessage
        {
            public Guid ClientId { get; set; }
            public Guid GetStreamId() => ClientId;
        }

        public class NewClient : Id, ICommand
        {
            public string Name { get; set; }
        }
        public class Created : NewClient, IEvent { }

        public class AddUserNotification : Id, ICommand
        {
            public string EmailAddress { get; set; }
        }
        public class UserNotificationAdded : AddUserNotification, IEvent { }

        public class RemoveUserNotification : Id, ICommand
        {
            public string EmailAddress { get; set; }
        }

        public class UserNotificationRemoved : RemoveUserNotification, IEvent { }

        public class Aggregate : AggregateBase,
            IHandleAggregateCommand<NewClient, UniqueNameValidator>,
            IHandleAggregateCommand<AddUserNotification>,
            IHandleAggregateCommand<RemoveUserNotification>
        {
            public void Handle(NewClient command, UniqueNameValidator uniqueNameValidator)
            {
                if (uniqueNameValidator.Dispatch(command).IsValid)
                    Raise<Created>(command);
            }

            public void Handle(AddUserNotification command)
            {
                if (notifications.Contains(command.EmailAddress))
                    throw new DomainAggregateException(this, "User already added!");
                Raise<UserNotificationAdded>(command);
            }

            public void Handle(RemoveUserNotification command)
            {
                if (!notifications.Contains(command.EmailAddress))
                    throw new DomainAggregateException(this, "Can't remove User that doesn't exist!");
                Raise<UserNotificationRemoved>(command);
            }

            //-------------------------------------------------------------------
            private List<string> notifications;

            private void Apply(Created e)
            {
                notifications = new List<string>();
            }

            private void Apply(UserNotificationAdded e)
            {
                notifications.Add(e.EmailAddress);
            }

            private void Apply(UserNotificationRemoved e)
            {
                notifications.Remove(e.EmailAddress);
            }
        }

        public class UniqueNameValidator : INeedExternalValidation<NewClient>
        {
            public CommandValidationResult Dispatch(NewClient command)
            {
                // Check ReadModel

                // .......
                return new CommandValidationResult(true);
            }
        }
    }
}