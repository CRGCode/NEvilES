using System;
using System.Collections.Generic;

namespace NEvilES.Abstractions.Pipeline
{
    public interface IReadEventStore
    {
        IEnumerable<IAggregateCommit> Read(long from = 0, long to = 0);
        IEnumerable<IAggregateCommit> Read(Guid streamId);
        IEnumerable<IAggregateCommit> ReadNewestLimit(Guid streamId, int limit = 50);
        IEnumerable<IAggregateCommit> ReadNewestLimit(int limit = 50);
    }
}