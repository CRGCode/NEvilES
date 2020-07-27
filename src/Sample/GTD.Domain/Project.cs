    using System;
using System.Collections.Generic;
using NEvilES;
using NEvilES.Abstractions;

namespace GTD.Domain
{
    public abstract class Project
    {
        public class NewProject : Created, ICommand { }
        public class Created : Event
        {
            public Guid ClientId { get; set; }
            public string Name { get; set; }
            public UserNotificationEndpoint[] DefaultContacts { get; set; }
        }

        public class CorrectProjectName : ProjectNameCorrected, ICommand { }
        public class ProjectNameCorrected : Event
        {
            public string NewName { get; set; }
        }

        public class InvolveUserInProject : UserInvolvedInProject, ICommand { }
        public class UserInvolvedInProject : Event
        {
            public UserNotificationEndpoint NotificationEndpoint { get; set; }
        }

        public class RemoveUserFromProject : UserRemovedFromProject, ICommand { }
        public class UserRemovedFromProject : Event
        {
            public UserNotificationEndpoint NotificationEndpoint { get; set; }
        }

        public enum NotificationType
        {
            Mobile,
            Email,
        }

        public class UserNotificationEndpoint
        {
            public UserNotificationEndpoint(Guid userId, NotificationType notificationType, string endpoint)
            {
                UserId = userId;
                NotificationType = notificationType;
                Endpoint = endpoint;
            }
            public Guid UserId { get; set; }
            public NotificationType NotificationType { get; set; }
            public string Endpoint { get; set; }
        }

        public class Aggregate : AggregateBase,
            IHandleAggregateCommand<NewProject, UniqueNameValidator>,
            IHandleAggregateCommand<InvolveUserInProject>,
            IHandleAggregateCommand<RemoveUserFromProject>
        {
            public ICommandResponse Handle(NewProject command, UniqueNameValidator uniqueNameValidator)
            {
                if (uniqueNameValidator.Dispatch(command).IsValid)
                {
                    RaiseEvent<Created>(command);
                }
                return new CommandCompleted(command.StreamId,nameof(RemoveUserFromProject));
            }

            public ICommandResponse Handle(InvolveUserInProject command)
            {
                if (notifications.Contains(command.NotificationEndpoint))
                    throw new DomainAggregateException(this, "Endpoint already added!");
                RaiseEvent<UserInvolvedInProject>(command);
                return new CommandCompleted(command.StreamId,nameof(RemoveUserFromProject));
            }

            public ICommandResponse Handle(RemoveUserFromProject command)
            {
                if (!notifications.Contains(command.NotificationEndpoint))
                    throw new DomainAggregateException(this, "Can't remove Endpoint that doesn't exist!");
                RaiseEvent<UserRemovedFromProject>(command);
                return new CommandCompleted(command.StreamId,nameof(RemoveUserFromProject));
            }

            //-------------------------------------------------------------------
            private HashSet<UserNotificationEndpoint> notifications;

            private void Apply(Created e)
            {
                notifications = new HashSet<UserNotificationEndpoint>(e.DefaultContacts);
            }

            private void Apply(UserInvolvedInProject e)
            {
                notifications.Add(e.NotificationEndpoint);
            }

            private void Apply(UserRemovedFromProject e)
            {
                notifications.Remove(e.NotificationEndpoint);
            }
        }

        public class UniqueNameValidator : INeedExternalValidation<NewProject>
        {
            public CommandValidationResult Dispatch(NewProject command)
            {
                // Check ReadModel
                return new CommandValidationResult(true);
            }
        }
    }
}