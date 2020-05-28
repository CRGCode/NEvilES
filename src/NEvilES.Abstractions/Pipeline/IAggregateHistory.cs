using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NEvilES.Abstractions.Pipeline
{
    public interface IAggregateHistory
    {
        IEnumerable<IAggregateCommit> Read(Int64 from = 0, Int64 to = 0);
        IEnumerable<IAggregateCommit> Read(Guid streamId);
        IEnumerable<IAggregateCommit> ReadNewestLimit(Guid streamId, int limit = 50);
        TAggregate GetVersion<TAggregate>(Guid id, Int64 version) where TAggregate : IAggregate;

    }
}