using System;
using System.Linq;
using LiteDB;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NEvilES.DataStore.LiteDb
{
    public class LiteDbEventStore : IRepository
    {
        private readonly LiteDatabase db;
        private readonly IEventTypeLookupStrategy eventTypeLookupStrategy;
        private readonly ICommandContext commandContext;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new JsonConverter[] { new StringEnumConverter() }
        };

        public LiteDbEventStore(
            LiteDatabase db,
            IEventTypeLookupStrategy eventTypeLookupStrategy,
            ICommandContext commandContext
        )
        {
            this.db = db;
            this.eventTypeLookupStrategy = eventTypeLookupStrategy;
            this.commandContext = commandContext;
        }

        public TAggregate Get<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            var aggregate = Get(typeof(TAggregate), id);
            return (TAggregate)aggregate;
        }

        public IAggregate Get(Type type, Guid id) => Get(type, id, null);

        public TAggregate GetVersion<TAggregate>(Guid id, long version) where TAggregate : IAggregate
        {
            return (TAggregate)Get(typeof(TAggregate), id, version);
        }

        public IAggregate Get(Type type, Guid id, long? version)
        {

            var collection = db.GetCollection<LiteDbEventTable>("eventstore");

            var events = collection.Find(Query.EQ(nameof(LiteDbEventTable.StreamId), new BsonValue(id))).ToList();

            if (events.Count == 0)
            {
                var emptyAggregate = (IAggregate)Activator.CreateInstance(type, true);
                ((AggregateBase)emptyAggregate).SetState(id);
                return emptyAggregate;
            }

            var aggregate = (IAggregate)Activator.CreateInstance(eventTypeLookupStrategy.Resolve(events[0].Category));

            foreach (var eventDb in events.OrderBy(x => x.Version))
            {
                var message =
                    (IEvent)
                    JsonConvert.DeserializeObject(eventDb.Body, eventTypeLookupStrategy.Resolve(eventDb.BodyType), SerializerSettings);
                //message.StreamId = eventDb.StreamId;
                aggregate.ApplyEvent(message);
            }
            ((AggregateBase)aggregate).SetState(id);

            return aggregate;
        }


        public IAggregate GetStateless(Type type, Guid id)
        {
            throw new NotImplementedException();
        }

        public IAggregateCommit Save(IAggregate aggregate)
        {
            if (aggregate.Id == Guid.Empty)
            {
                throw new Exception(
                    $"The aggregate {aggregate.GetType().FullName} has tried to be saved with an empty id");
            }

            var uncommittedEvents = aggregate.GetUncommittedEvents().Cast<IEventData>().ToArray();

            try
            {
                var col = db.GetCollection<LiteDbEventTable>("eventstore");
                col.EnsureIndex(x => x.StreamId, false);

                col.InsertBulk(uncommittedEvents.Select(x => new LiteDbEventTable
                {
                    StreamId = aggregate.Id,
                    Version = x.Version,
                    TransactionId = commandContext.Transaction.Id,
                    AppVersion = commandContext.AppVersion,
                    When = x.TimeStamp,
                    Body = JsonConvert.SerializeObject(x.Event, SerializerSettings),
                    Category = aggregate.GetType().FullName,
                    Who = commandContext.ImpersonatorBy?.GuidId ?? commandContext.By.GuidId,
                    BodyType = x.Type.FullName
                }));
            }
            catch (Exception)
            {
                throw new Exception(
                    $"The aggregate {aggregate.GetType().FullName} has tried to save events to an old version of an aggregate");
            }

            aggregate.ClearUncommittedEvents();
            return new AggregateCommit(aggregate.Id, commandContext.By.GuidId, uncommittedEvents);
        }
    }
}