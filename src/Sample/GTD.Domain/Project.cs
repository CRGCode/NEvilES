using System;
using System.Collections.Generic;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace GTD.Domain
{
    public abstract class Project
    {
        public abstract class Id : IMessage
        {
            public Guid GetStreamId() => ProjectId;
            public Guid ProjectId { get; set; }
        }

        public class NewProject : Id, ICommand
        {
            public Guid ClientId { get; set; }
            public string Name { get; set; }
            public UserNotificationEndpoint[] DefaultContacts { get; set; }
        }

        public class Created : NewProject, IEvent { }

        public class CorrectProjectName : Id, ICommand
        {
            public string NewName { get; set; }
        }
        public class ProjectNameCorrected : CorrectProjectName, IEvent { }

        public class InvolveUserInProject : Id, ICommand
        {
            public UserNotificationEndpoint NotificationEndpoint { get; set; }
        }
        public class UserInvolvedInProject : InvolveUserInProject, IEvent { }

        public class RemoveUserFromProject : Id, ICommand
        {
            public UserNotificationEndpoint NotificationEndpoint { get; set; }
        }
        public class UserRemovedFromProject : RemoveUserFromProject, IEvent { }

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
            public void Handle(NewProject command, UniqueNameValidator uniqueNameValidator)
            {
                if (uniqueNameValidator.Dispatch(command).IsValid)
                    Raise<Created>(command);
            }

            public void Handle(InvolveUserInProject command)
            {
                if (notifications.Contains(command.NotificationEndpoint))
                    throw new DomainAggregateException(this, "Endpoint already added!");
                Raise<UserInvolvedInProject>(command);
            }

            public void Handle(RemoveUserFromProject command)
            {
                if (!notifications.Contains(command.NotificationEndpoint))
                    throw new DomainAggregateException(this, "Can't remove Endpoint that doesn't exist!");
                Raise<UserRemovedFromProject>(command);
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