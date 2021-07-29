﻿using System;
using System.Collections.Generic;
using NEvilES.Abstractions.Pipeline;
using Newtonsoft.Json;

namespace GTD.ReadModel
{
    public class Request : IHaveIdentity
    {
        [JsonConstructor]
        public Request(Guid id, Guid projectId, string shortName, string description, int priority, Comment[] comments)
        {
            Id = id;
            ProjectId = projectId;
            ShortName = shortName;
            Description = description;
            Priority = priority;
            Comments = comments == null ? new List<Comment>() : new List<Comment>(comments);
        }

        public Request(Domain.Request.Created c)
        {
            Id = c.StreamId;
            ProjectId = c.ProjectId;
            ShortName = c.ShortName;
            Description = c.Description;
            Priority = c.Priority;
        }

        public Guid Id { get; }
        public Guid ProjectId { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; }
        public List<Comment> Comments { get; set; }

        public class Comment
        {
            public Comment(Guid id, string text)
            {
                Id = id;
                Text = text;
            }

            public Guid Id { get; }
            public string Text { get; set; }
            public Comment(Domain.Request.CommentAdded c)
            {
                Id = c.StreamId;
                Text = c.Text;
            }
        }

        public class Projector :
            IProject<Domain.Request.Created>,
            IProject<Domain.Request.CommentAdded>
        {
            private readonly IReadFromReadModel reader;
            private readonly IWriteReadModel writer;

            public Projector(IReadFromReadModel reader, IWriteReadModel writer)
            {
                this.reader = reader;
                this.writer = writer;
            }

            public void Project(Domain.Request.Created message, IProjectorData data)
            {
                writer.Insert(new Request(message));
            }

            public void Project(Domain.Request.CommentAdded message, IProjectorData data)
            {
                var request = reader.Get<Request>(message.StreamId);
                request.Comments.Add(new Comment(message));
                writer.Update(request);
            }
        }
    }
}