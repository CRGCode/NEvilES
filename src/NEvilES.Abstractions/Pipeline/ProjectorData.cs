using System;
using System.Collections.Generic;
using System.Linq;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public class ProjectorResult : IProjectorResult
    {
        public object[] Items { get; }

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

        public Guid StreamId { get; }
        public ICommandContext CommandContext { get; set; }
        public IUser By => CommandContext.By;
        public Type Type { get; }
        public object Event { get; }
        public DateTime TimeStamp { get; }
        public int Version { get; }
    }
    
    public class ProjectorException : Exception
    {
        public ProjectorException(Exception exception, string message, params object[] args)
            : base(string.Format(message, args), exception)
        {
        }
    }
}