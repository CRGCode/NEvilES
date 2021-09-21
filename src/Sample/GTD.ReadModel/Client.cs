using System;
using System.Collections.Generic;
using System.Linq;
using NEvilES.Abstractions.Pipeline;

namespace GTD.ReadModel
{
    public class Client : IHaveIdentity<Guid>
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
            private readonly IReadFromReadModel<Guid> reader;
            private readonly IWriteReadModel<Guid> writer;

            public Projector(IReadFromReadModel<Guid> reader, IWriteReadModel<Guid> writer)
            {
                this.reader = reader;
                this.writer = writer;
            }

            public void Project(Domain.Client.Created message, IProjectorData data)
            {
                writer.Insert(new Client(message.ClientId, message.Name));
            }

            public void Project(Domain.Client.UserNotificationAdded message, IProjectorData data)
            {
                var client = reader.Get<Client>(message.ClientId);
                client.NotificationEndPoints.Add(new NotificationEndPoint(message.EmailAddress));
                writer.Update(client);
            }

            public void Project(Domain.Client.UserNotificationRemoved message, IProjectorData data)
            {
                var client = reader.Get<Client>(message.ClientId);
                client.NotificationEndPoints.Remove(client.NotificationEndPoints.First(x => x.EmailAddress == message.EmailAddress));
                writer.Update(client);
            }
        }
    }
}
