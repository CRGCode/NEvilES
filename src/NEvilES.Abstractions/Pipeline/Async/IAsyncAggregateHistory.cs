using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NEvilES.Abstractions.Pipeline.Async
{
    public interface IAsyncAggregateHistory
    {
        Task<IEnumerable<IAggregateCommit>> ReadAsync(Int64 from = 0, Int64 to = 0);
        Task<IEnumerable<IAggregateCommit>> ReadAsync(Guid streamId);
        Task<IEnumerable<IAggregateCommit>> ReadNewestLimitAsync(Guid streamId, int limit = 50);
        Task<TAggregate> GetVersionAsync<TAggregate>(Guid id, Int64 version) where TAggregate : IAggregate;
    }
}