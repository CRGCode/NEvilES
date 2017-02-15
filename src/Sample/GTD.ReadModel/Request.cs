using System;
using System.Collections.Generic;
using System.Linq;
using GTD.Common;
using NEvilES.Pipeline;

namespace GTD.ReadModel
{
    public class Request : IHaveIdentity
    {
        public Request(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid Id { get; }
        public string Name { get; }

        public class Projector :
            IProject<Domain.Request.Created>,
            IProject<Domain.Request.CommentAdded>
        {
            private readonly IReadData reader;
            private readonly IWriteData writer;

            public Projector(IReadData reader, IWriteData writer)
            {
                this.reader = reader;
                this.writer = writer;
            }

            public void Project(Domain.Request.Created message, ProjectorData data)
            {
                //writer.Insert(new Client(message.StreamId, message.Name));
            }

            public void Project(Domain.Request.CommentAdded message, ProjectorData data)
            {
                //var client = reader.Get<Client>(message.StreamId);
                //client.NotificationEndPoints.Remove(client.NotificationEndPoints.First(x => x.EmailAddress == message.EmailAddress));
                //writer.Update(client);
            }
        }
    }
}