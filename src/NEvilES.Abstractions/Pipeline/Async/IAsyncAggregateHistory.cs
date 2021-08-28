using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NEvilES.Abstractions.Pipeline.Async
{
    public interface IAsyncAggregateHistory
    {
        Task<IEnumerable<IAggregateCommit>> ReadAsync(long from = 0, long to = 0);
        Task<IEnumerable<IAggregateCommit>> ReadAsync(Guid streamId);
        Task<IEnumerable<IAggregateCommit>> ReadNewestLimitAsync(Guid streamId, int limit = 50);
        Task<TAggregate> GetVersionAsync<TAggregate>(Guid id, long version) where TAggregate : IAggregate;
    }
}