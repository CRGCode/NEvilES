using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public class TestRepository : IRepository, IAsyncRepository
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

        public async Task<TAggregate> GetAsync<TAggregate>(Guid id) where TAggregate : IAggregate
            => (TAggregate)(await (GetAsync(typeof(TAggregate), id)));

        public Task<IAggregate> GetAsync(Type type, Guid id)
            => GetAsync(type, id,null);

        public Task<IAggregate> GetAsync(Type type, Guid id, long? version)
            => Task.FromResult(Get(type,id));



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

        public TAggregate GetVersion<TAggregate>(Guid id, long version) where TAggregate : IAggregate
        {
            throw new NotImplementedException();
        }

        public Task<IAggregateCommit> SaveAsync(IAggregate aggregate)
            => Task.FromResult(Save(aggregate));

        public IAggregateCommit Save(IAggregate aggregate)
        {
            aggregates[aggregate.Id] = aggregate;
            return new AggregateCommit(aggregate.Id, Guid.Empty, aggregate.GetUncommittedEvents().Cast<IEventData>().ToArray());
        }

        public class DomainAggregateDoesNotExist : Exception
        {
            public DomainAggregateDoesNotExist(Guid id)
                : base($"Domain aggregate doesn't exist {id}")
            {
            }
        }

        public IAggregate GetStateless(Type type, Guid id)
        {
            return null;
        }

        public Task<IAggregate> GetStatelessAsync(Type type, Guid id)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}