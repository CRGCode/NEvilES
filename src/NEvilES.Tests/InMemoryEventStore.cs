using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NEvilES.DataStore;

namespace NEvilES.Tests
{ 
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

        public IAggregate Get(Type type, Guid id)
        {
            if (!eventData.ContainsKey(id))
            {
                var emptyAggregate = (IAggregate)Activator.CreateInstance(type, true);
                ((AggregateBase)emptyAggregate).SetState(id);
                return emptyAggregate;
            }
            var evts = eventData[id].OrderBy(x => x.Id).ToArray();

            var aggregate = (IAggregate)Activator.CreateInstance(eventTypeLookupStrategy.Resolve(evts[0].Category));

            foreach (var eventDb in evts.OrderBy(x => x.Version))
            {
                var message = (IEvent)JsonConvert.DeserializeObject(eventDb.Body, eventDb.BodyType);
                message.StreamId = eventDb.StreamId;
                aggregate.ApplyEvent(message);
            }
            ((AggregateBase)aggregate).SetState(id);

            return aggregate;
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

        public AggregateCommit Save(IAggregate aggregate)
        {
            if (aggregate.Id == Guid.Empty)
            {
                throw new Exception($"The aggregate {aggregate.GetType().FullName} has tried to be saved will an empty id");
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
            return new AggregateCommit(aggregate.Id, Guid.Empty, "", uncommittedEvents);
        }
    }
}