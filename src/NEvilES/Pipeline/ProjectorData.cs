using System;
using System.Collections.Generic;
using System.Linq;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public class ProjectorResult : IProjectorResult
    {
        public object[] Items { get; private set; }

        public ProjectorResult(IEnumerable<object> updatedObjects)
        {
            Items = updatedObjects.ToArray();
        }

        public ProjectorResult(params object[] updatedObjects) : this((IEnumerable<object>)updatedObjects)
        {
        }
    }

    public class ProjectorData : IProjectorData
    {
        public ProjectorData(Guid streamId, ICommandContext commandContext, Type type, object @event, DateTime timeStamp, int version)
        {
            StreamId = streamId;
            CommandContext = commandContext;
            Type = type;
            Event = @event;
            TimeStamp = timeStamp;
            Version = version;
        }

        public Guid StreamId { get; private set; }
        public ICommandContext CommandContext { get; set; }
        public IUser By => CommandContext.By;
        public Type Type { get; private set; }
        public object Event { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public int Version { get; private set; }
        public Guid TranId => CommandContext.Transaction.Id;
    }
}