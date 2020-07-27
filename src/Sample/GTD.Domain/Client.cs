using System;
using System.Collections.Generic;
using NEvilES;
using NEvilES.Abstractions;

namespace GTD.Domain
{
    public abstract class Client
    {
        public class NewClient : Created, ICommand { }
        public class Created : Event
        {
            public string Name { get; set; }
        }

        public class AddUserNotification : UserNotificationAdded, ICommand { }
        public class UserNotificationAdded : Event
        {
            public string EmailAddress { get; set; }
        }

        public class RemoveUserNotification : UserNotificationRemoved, ICommand { }
        public class UserNotificationRemoved : Event
        {
            public string EmailAddress { get; set; }
        }

        public class Aggregate : AggregateBase,
            IHandleAggregateCommand<NewClient, UniqueNameValidator>,
            IHandleAggregateCommand<AddUserNotification>,
            IHandleAggregateCommand<RemoveUserNotification>
        {
            public ICommandResponse Handle(NewClient command, UniqueNameValidator uniqueNameValidator)
            {
                if (uniqueNameValidator.Dispatch(command).IsValid)
                {
                    RaiseEvent<Created>(command);
                    return new CommandCompleted(command.StreamId,nameof(NewClient));
                }
                return new CommandRejectedWithError<string>(command.StreamId,nameof(NewClient),"");
            }

            public ICommandResponse Handle(AddUserNotification command)
            {
                if (notifications.Contains(command.EmailAddress))
                    throw new DomainAggregateException(this, "User already added!");
                RaiseEvent<UserNotificationAdded>(command);
                return new CommandCompleted(command.StreamId, nameof(AddUserNotification));
            }

            public ICommandResponse Handle(RemoveUserNotification command)
            {
                if (!notifications.Contains(command.EmailAddress))
                    throw new DomainAggregateException(this, "Can't remove User that doesn't exist!");
                RaiseEvent<UserNotificationRemoved>(command);
                return new CommandCompleted(command.StreamId, nameof(RemoveUserNotification));
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
                return new CommandValidationResult(true);
            }
        }
    }
}