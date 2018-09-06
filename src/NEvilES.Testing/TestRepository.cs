using System;
using System.Collections.Generic;
using System.Linq;
using NEvilES.Abstractions;

namespace NEvilES.Testing
{
    public abstract class Given
    {
        public Given(Guid streamId, Type type, params IEvent[] events)
        {
            History = new List<IEvent>(events);
            StreamId = streamId;
            AggregateType = type;
        }

        public Guid StreamId { get; set; }
        public Type AggregateType { get; set; }
        public List<IEvent> History { get; set; }

        public static Given<T> EmptyStream<T>(Guid streamId) where T : AggregateBase
        {
            return new Given<T>(streamId, typeof(T));
        }

        public static Given<T> From<T>(Guid streamId, params IEvent[] events) where T : AggregateBase
        {
            return new Given<T>(streamId, typeof(T), events);
        }

        public static Given<T> From<T>(Guid streamId, IEnumerable<IEvent> events) where T : AggregateBase
        {
            return new Given<T>(streamId, typeof(T), events.ToArray());
        }
    }

    public class Given<T> : Given where T : AggregateBase
    {
        public Given(Guid streamId, Type type, params IEvent[] events)
            : base(streamId, type, events)
        {
        }

        public static Given<T> EmptyStream(Guid streamId)
        {
            return new Given<T>(streamId, typeof(T));
        }

        public static Given<T> From(Guid streamId, params IEvent[] events)
        {
            return new Given<T>(streamId, typeof(T), events);
        }

        public static Given<T> From(Guid streamId, IEnumerable<IEvent> events)
        {
            return new Given<T>(streamId, typeof(T), events.ToArray());
        }
    }

    public class TestRepository : IRepository
    {
        private readonly Dictionary<Guid, IAggregate> aggregates;
        private readonly Dictionary<Guid, Given> given;

        public TestRepository() : this(new Dictionary<Guid, Given>())
        {

        }

        public TestRepository(Dictionary<Guid, Given> existingEvents)
        {
            aggregates = new Dictionary<Guid, IAggregate>();
            given = existingEvents;
        }

        public TAggregate Get<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            return (TAggregate)Get(typeof(TAggregate), id);
        }

        public IAggregate Get(Type type, Guid id)
        {
            IAggregate aggregate;
            if (!aggregates.TryGetValue(id, out aggregate))
            {
                if (given.ContainsKey(id))
                {
                    aggregate = (IAggregate)Activator.CreateInstance(given[id].AggregateType, true);
                    foreach (var e in given[id].History)
                        aggregate.ApplyEvent(e);
                }
                else
                {
                    throw new Exception("Failed trying to retrieve an aggregate that doesn't exist in the given");
                }

                aggregates.Add(id, aggregate);
            }

            ((AggregateBase)aggregate).SetState(id);
            return aggregate;
        }

        public AggregateCommit Save(IAggregate aggregate)
        {
            aggregates[aggregate.Id] = aggregate;
            return new AggregateCommit(aggregate.Id, Guid.Empty, "", aggregate.GetUncommittedEvents().Cast<IEventData>().ToArray());
        }

        public class DomainAggregateDoesNotExist : Exception
        {
            public DomainAggregateDoesNotExist(Guid id)
                : base(string.Format("Domain aggregate doesn't exist {0}", id))
            {
            }
        }

        public IAggregate GetStateless(Type type, Guid id)
        {
            return null;
        }

        IAggregateCommit IRepository.Save(IAggregate aggregate)
        {
            throw new NotImplementedException();
        }
    }
}