using System;
using System.Collections.Generic;

namespace NEvilES.Abstractions.Pipeline
{
    public interface IReadEventStore
    {
        IEnumerable<IAggregateCommit> Read(Int64 from = 0, Int64 to = 0);
        IEnumerable<IAggregateCommit> Read(Guid streamId);
        IEnumerable<IAggregateCommit> ReadNewestLimit(Guid streamId, int limit = 50);
    }
}