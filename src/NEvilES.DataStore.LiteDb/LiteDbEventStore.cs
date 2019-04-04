using System;
using System.Collections.Generic;
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
        private readonly LiteDatabase _db;
        private readonly IEventTypeLookupStrategy _eventTypeLookupStrategy;
        private readonly ICommandContext _commandContext;
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
            _db = db;
            _eventTypeLookupStrategy = eventTypeLookupStrategy;
            _commandContext = commandContext;
        }

        public TAggregate Get<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            IAggregate aggregate = Get(typeof(TAggregate), id);
            return (TAggregate)aggregate;
        }

        public IAggregate Get(Type type, Guid id) => Get(type, id, null);

        public IAggregate Get(Type type, Guid id, Int64? version)
        {

            var collection = _db.GetCollection<LiteDbEventTable>("eventstore");

            var events = collection.Find(Query.EQ(nameof(LiteDbEventTable.StreamId), new BsonValue(id))).ToList();

            if (events.Count == 0)
            {
                var emptyAggregate = (IAggregate)Activator.CreateInstance(type, true);
                ((AggregateBase)emptyAggregate).SetState(id);
                return emptyAggregate;
            }

            var aggregate = (IAggregate)Activator.CreateInstance(_eventTypeLookupStrategy.Resolve(events[0].Category));

            foreach (var eventDb in events.OrderBy(x => x.Version))
            {
                var message =
                    (IEvent)
                    JsonConvert.DeserializeObject(eventDb.Body, _eventTypeLookupStrategy.Resolve(eventDb.BodyType), SerializerSettings);
                message.StreamId = eventDb.StreamId;
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
            var count = 0;

            var metadata = string.Empty;
            try
            {

                // while (count < uncommittedEvents.Length)
                // {
                    // var items = new List<TransactWriteItem>();

                    // foreach (var eventData in uncommittedEvents.Skip(count).Take(10))
                    // {
                    //     int eventVersion = (aggregate.Version - uncommittedEvents.Length + count + 1);
                    //     // items.Add(GetDynamoDbTransactItem(aggregate, eventVersion, metadata, eventData));
                    //     count++;
                    // }

                    var col = _db.GetCollection<LiteDbEventTable>("eventstore");


                    // var abc = await _dynamoDbClient.TransactWriteItemsAsync(new Amazon.DynamoDBv2.Model.TransactWriteItemsRequest
                    // {
                    //     TransactItems = items
                    // });

                // }
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"The aggregate {aggregate.GetType().FullName} has tried to save events to an old version of an aggregate");
            }

            aggregate.ClearUncommittedEvents();
            return new AggregateCommit(aggregate.Id, _commandContext.By.GuidId, metadata, uncommittedEvents);
        }
    }
}