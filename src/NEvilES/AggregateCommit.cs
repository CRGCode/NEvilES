using System;
using NEvilES.Abstractions;

namespace NEvilES
{
    public class AggregateCommit : BaseAggregateCommit
    {
        public AggregateCommit(Guid streamId, Guid by, IEventData[] updatedEvents)
        {
            StreamId = streamId;
            By = by;
            UpdatedEvents = updatedEvents;
        }
    }
}