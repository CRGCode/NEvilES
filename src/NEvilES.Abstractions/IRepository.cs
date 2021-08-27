using System;
using System.Threading.Tasks;

namespace NEvilES.Abstractions
{
    public interface IRepository
    {
        TAggregate Get<TAggregate>(Guid id) where TAggregate : IAggregate;
        IAggregate Get(Type type, Guid id);
        TAggregate GetVersion<TAggregate>(Guid id, Int64 version) where TAggregate : IAggregate;
        IAggregate GetStateless(Type type, Guid id);
        IAggregateCommit Save(IAggregate aggregate);
    }

    public interface IAsyncRepository
    {
        Task<TAggregate> GetAsync<TAggregate>(Guid id) where TAggregate : IAggregate;
        Task<IAggregate> GetAsync(Type type, Guid id);
        Task<IAggregate> GetAsync(Type type, Guid id, Int64? version);
        Task<IAggregate> GetStatelessAsync(Type type, Guid id);
        Task<IAggregateCommit> SaveAsync(IAggregate aggregate);
    }
}