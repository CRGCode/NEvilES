using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NEvilES.Abstractions;

namespace NEvilES.Tests
{
    public class InMemoryAggregateEventStore : IRepository, IAsyncRepository
    {
        private readonly Dictionary<Guid, IAggregate> aggregates;

        public InMemoryAggregateEventStore()
        {
            aggregates = new Dictionary<Guid, IAggregate>();
        }

        public IAggregateCommit Save(IAggregate aggregate)
        {
            if (aggregate.Id == Guid.Empty)
            {
                throw new Exception($"The aggregate {aggregate.GetType().FullName} has tried to be saved with an empty id");
            }

            if (!aggregates.ContainsKey(aggregate.Id))
            {
                aggregates.Add(aggregate.Id, aggregate);
            }

            var events = aggregate.GetUncommittedEvents().Cast<IEventData>().ToArray();

            aggregate.ClearUncommittedEvents();
            return new AggregateCommit(aggregate.Id, Guid.Empty, events);
        }

        public TAggregate Get<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            return (TAggregate)Get(typeof(TAggregate), id);
        }

        public IAggregate Get(Type type, Guid id)
        {
            if (aggregates.ContainsKey(id))
                return aggregates[id];

            var aggregate = (IAggregate) Activator.CreateInstance(type, true);
            ((AggregateBase)aggregate).SetState(id);

            aggregates.Add(id, aggregate);
            return aggregate;
        }

        public TAggregate GetVersion<TAggregate>(Guid id, long version) where TAggregate : IAggregate
        {
            // we can't get the version in this implementation
            throw new NotImplementedException();
        }

        public IAggregate GetStateless(Type type, Guid id)
        {
            if (aggregates.ContainsKey(id))
                return aggregates[id];

            if (type == null)
            {
                throw new Exception($"Attempt to get stateless instance of a non-constructable aggregate with stream: {id}");
            }

            var aggregate = (IAggregate)Activator.CreateInstance(type, true);
            ((AggregateBase)aggregate).SetState(id);

            aggregates.Add(id, aggregate);
            return aggregate;
        }

        public Task<TAggregate> GetAsync<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            return Task.FromResult(Get<TAggregate>(id));
        }

        public Task<IAggregate> GetAsync(Type type, Guid id)
        {
            return Task.FromResult(Get(type, id));
        }

        public Task<IAggregate> GetAsync(Type type, Guid id, long? version)
        {
            // we can't get the version in this implementation
            throw new NotImplementedException();
        }

        public Task<IAggregate> GetStatelessAsync(Type type, Guid id)
        {
            return Task.FromResult(GetStateless(type, id));
        }

        public Task<IAggregateCommit> SaveAsync(IAggregate aggregate)
        {
            return Task.FromResult(Save(aggregate));
        }
    }
}