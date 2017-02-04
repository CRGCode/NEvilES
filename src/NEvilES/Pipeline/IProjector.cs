using System;
using System.Collections.Generic;
using System.Linq;

namespace NEvilES.Pipeline
{
    public interface IProject<in T> where T : IEvent
    {
        void Project(T message, ProjectorData data);
    }

    public interface IProjectWithResult<in T> where T : IEvent
    {
        ProjectorResult Project(T message, ProjectorData data);
    }

    public class ProjectorResult
    {
        public object[] Items { get; private set; }

        public ProjectorResult(IEnumerable<object> updatedObjects)
        {
            Items = updatedObjects.ToArray();
        }

        public ProjectorResult(params object[] updatedObjects) : this((IEnumerable <object>)updatedObjects)
        {
        }
    }

    public class ProjectorData
    {
        public ProjectorData(Guid streamId, CommandContext commandContext, string type, object @event, DateTime timeStamp, int version)
        {
            StreamId = streamId;
            CommandContext = commandContext;
            Type = type;
            Event = @event;
            TimeStamp = timeStamp;
            Version = version;
        }

        public Guid StreamId { get; private set; }
        public CommandContext CommandContext { get; set; }
        public CommandContext.User By => CommandContext.By;
        public string Type { get; private set; }
        public object Event { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public int Version { get; private set; }
        public Guid TranId => CommandContext.TransactionId;
    }
}
