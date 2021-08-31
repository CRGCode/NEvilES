using System;
using System.Collections.Generic;
using System.Linq;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NEvilES
{
    public class Transaction : ITransaction
    {
        public Guid Id { get; }

        public void Rollback()
        {
            throw new NotImplementedException();
        }

        public Transaction(Guid id)
        {
            Id = id;
        }
    }

    public class InMemoryEventStore : IRepository
    {
        private class EventDb
        {
            public int Id { get; set; }
            public string Category { get; set; }
            public Guid StreamId { get; set; }
            public Type BodyType { get; set; }
            public string Body { get; set; }
            public int Version { get; set; }
        }

        private readonly Dictionary<Guid, List<EventDb>> eventData;

        private readonly IEventTypeLookupStrategy eventTypeLookupStrategy;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new JsonConverter[] { new StringEnumConverter() }
        };

        public InMemoryEventStore(IEventTypeLookupStrategy eventTypeLookupStrategy)
        {
            eventData = new Dictionary<Guid, List<EventDb>>();

            this.eventTypeLookupStrategy = eventTypeLookupStrategy;
        }

        public TAggregate Get<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            return (TAggregate)Get(typeof(TAggregate), id);
        }
        public IAggregate Get(Type type, Guid id) => Get(type, id, null);

        public IAggregate Get(Type type, Guid id, long? version)
        {
            if (!eventData.ContainsKey(id))
            {
                var emptyAggregate = (IAggregate)Activator.CreateInstance(type, true);
                ((AggregateBase)emptyAggregate).SetState(id);
                return emptyAggregate;
            }

            var events = version.HasValue ? 
                eventData[id].Where(x => x.Version <= version).OrderBy(x => x.Id).ToArray() : 
                eventData[id].OrderBy(x => x.Id).ToArray();

            var aggregate = (IAggregate)Activator.CreateInstance(eventTypeLookupStrategy.Resolve(events[0].Category));

            foreach (var eventDb in events.OrderBy(x => x.Version))
            {
                var message = (IEvent)JsonConvert.DeserializeObject(eventDb.Body, eventTypeLookupStrategy.Resolve(eventDb.BodyType.FullName), SerializerSettings);
                message.StreamId = eventDb.StreamId;
                aggregate.ApplyEvent(message);
            }
            ((AggregateBase)aggregate).SetState(id);

            return aggregate;
        }

        public TAggregate GetVersion<TAggregate>(Guid id, long version) where TAggregate : IAggregate
        {
            return (TAggregate)Get(typeof(TAggregate), id, version);
        }

        public IAggregate GetStateless(Type type, Guid id)
        {
            IAggregate aggregate;

            EventDb eventDb = null;

            List<EventDb> events;
            if (eventData.TryGetValue(id, out events))
            {
                eventDb = events
                    .Take(1)
                    .OrderByDescending(x => x.Id)
                    .SingleOrDefault();
            }

            if (eventDb == null)
            {
                if (type == null)
                {
                    throw new Exception($"Attempt to get stateless instance of a non-constructable aggregate with stream: {id}");
                }

                aggregate = (IAggregate)Activator.CreateInstance(type, true);
            }
            else
            {
                aggregate = (IAggregate)Activator.CreateInstance(eventTypeLookupStrategy.Resolve(eventDb.Category));
            }
            ((AggregateBase)aggregate).SetState(id, eventDb?.Version ?? 0);

            return aggregate;
        }

        public IAggregateCommit Save(IAggregate aggregate)
        {
            if (aggregate.Id == Guid.Empty)
            {
                throw new Exception($"The aggregate {aggregate.GetType().FullName} has tried to be saved with an empty id");
            }

            var uncommittedEvents = aggregate.GetUncommittedEvents().Cast<IEventData>().ToArray();
            var count = 0;

            List<EventDb> events;
            if (!eventData.TryGetValue(aggregate.Id, out events))
            {
                events = new List<EventDb>();
                eventData.Add(aggregate.Id, events);
            }
            foreach (var uncommittedEvent in uncommittedEvents)
            {
                var version = aggregate.Version - uncommittedEvents.Length + count + 1;
                var dbEvent = new EventDb()
                {
                    Id = version,
                    StreamId = aggregate.Id,
                    Body = JsonConvert.SerializeObject(uncommittedEvent.Event, SerializerSettings),
                    Category = aggregate.GetType().FullName,
                    BodyType = uncommittedEvent.Type,
                    Version = version,
                };

                events.Add(dbEvent);
                count++;
            }

            aggregate.ClearUncommittedEvents();
            return new AggregateCommit(aggregate.Id, Guid.Empty, uncommittedEvents);
        }
    }
}