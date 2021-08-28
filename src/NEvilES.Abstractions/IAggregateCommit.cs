using System;
using System.Linq;

namespace NEvilES.Abstractions
{
    public interface IAggregateCommit
    {
        Guid StreamId { get; set; }
        Guid By { get; set; }
        IEventData[] UpdatedEvents { get; set; }
        string ToString();
    }

    public abstract class BaseAggregateCommit : IAggregateCommit
    {
        public Guid StreamId { get; set; }
        public Guid By { get; set; }
        public IEventData[] UpdatedEvents { get; set; }

        public override string ToString()
        {
            return string.Join(",", UpdatedEvents.Select(x => x.Event));
        }
    }
}