using System;
using System.Collections.Generic;
using System.Linq;
using GTD.Common;
using NEvilES.Pipeline;

namespace GTD.ReadModel
{
    public class Client : IHaveIdentity
    {
        public Client(Guid id, string name)
        {
            Id = id;
            Name = name;
            NotificationEndPoints = new List<NotificationEndPoint>();
        }
        public Guid Id { get; }
        public string Name { get; }
        public List<NotificationEndPoint> NotificationEndPoints { get; set; }

        public class NotificationEndPoint
        {
            public NotificationEndPoint(string emailAddress)
            {
                EmailAddress = emailAddress;
            }

            public string EmailAddress { get; set; }
        }

        public class Projector :
            IProject<Domain.Client.Created>,
            IProject<Domain.Client.UserNotificationAdded>,
            IProject<Domain.Client.UserNotificationRemoved>
        {
            private readonly IReadFromReadModel reader;
            private readonly IWriteReadModel writer;

            public Projector(IReadFromReadModel reader, IWriteReadModel writer)
            {
                this.reader = reader;
                this.writer = writer;
            }

            public void Project(Domain.Client.Created message, ProjectorData data)
            {
                writer.Insert(new Client(message.StreamId, message.Name));
            }

            public void Project(Domain.Client.UserNotificationAdded message, ProjectorData data)
            {
                var client = reader.Get<Client>(message.StreamId);
                client.NotificationEndPoints.Add(new NotificationEndPoint(message.EmailAddress));
                writer.Update(client);
            }

            public void Project(Domain.Client.UserNotificationRemoved message, ProjectorData data)
            {
                var client = reader.Get<Client>(message.StreamId);
                client.NotificationEndPoints.Remove(client.NotificationEndPoints.First(x => x.EmailAddress == message.EmailAddress));
                writer.Update(client);
            }
        }
    }
}
