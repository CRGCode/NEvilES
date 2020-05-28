using System;
using System.Linq;
using NEvilES.Abstractions;

namespace NEvilES
{
    public class AggregateCommit : BaseAggregateCommit
    {
        public AggregateCommit(Guid streamId, Guid by, string metadata, IEventData[] updatedEvents)
        {
            StreamId = streamId;
            By = by;
            Metadata = metadata;
            UpdatedEvents = updatedEvents;
        }
    }
}