using System;
using System.Collections.Generic;
using System.Linq;

namespace NEvilES.Tests
{
    public class InMemoryAggregateEventStore : IRepository
    {
        private readonly Dictionary<Guid, IAggregate> aggregates;

        public InMemoryAggregateEventStore()
        {
            aggregates = new Dictionary<Guid, IAggregate>();
        }

        public AggregateCommit Save(IAggregate aggregate)
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
            return new AggregateCommit(aggregate.Id, Guid.Empty, string.Empty, events);
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
    }
}