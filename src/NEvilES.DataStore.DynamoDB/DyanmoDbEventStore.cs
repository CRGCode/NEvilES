using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Abstractions.Pipeline.Async;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NEvilES.DataStore.DynamoDB
{
    public class DynamoDbEventStore : IAsyncRepository, IAsyncAggregateHistory
    {
        private readonly IDynamoDBContext context;
        private readonly IAmazonDynamoDB dynamoDbClient;
        private readonly IEventTypeLookupStrategy eventTypeLookupStrategy;
        private readonly ICommandContext commandContext;

        public static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new JsonConverter[] { new StringEnumConverter() }
        };

        public DynamoDbEventStore(
            IAmazonDynamoDB dynamoDbClient,
            IEventTypeLookupStrategy eventTypeLookupStrategy,
            ICommandContext commandContext
        )
        {
            this.dynamoDbClient = dynamoDbClient;
            context = new DynamoDBContext(this.dynamoDbClient);  // TODO this looks dodgy use DI scopes and register as singleton
            this.eventTypeLookupStrategy = eventTypeLookupStrategy;
            this.commandContext = commandContext;
        }

        public async Task<TAggregate> GetAsync<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            IAggregate aggregate = await GetAsync(typeof(TAggregate), id);
            return (TAggregate)aggregate;
        }
        public Task<IAggregate> GetAsync(Type type, Guid id) => GetAsync(type, id, null);

        public async Task<IAggregate> GetAsync(Type type, Guid id, long? version)
        {
            var expression = new Expression()
            {
                ExpressionStatement = $"{nameof(DynamoDBEvent.StreamId)} = :sId",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
                    {":sId",id.ToString()},
                    {":v",  version.HasValue ? version.Value : 0}
                }
            };

            if (version.HasValue && version.Value > 0)
            {
                expression.ExpressionStatement += $" AND {nameof(DynamoDBEvent.Version)} <= :v";
            }
            else
            {
                expression.ExpressionStatement += $" AND {nameof(DynamoDBEvent.Version)} >= :v";
            }

            var query = context.FromQueryAsync<DynamoDBEvent>(new QueryOperationConfig()
            {
                ConsistentRead = true,
                KeyExpression = expression
            });

            var events = await query.GetRemainingAsync();

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

        public async Task<IAggregate> GetStatelessAsync(Type type, Guid id)
        {
            IAggregate aggregate;

            int? version = null;
            string category = null;

            var expression = new Expression()
            {
                ExpressionStatement = $"{nameof(DynamoDBEvent.StreamId)} = :sId  AND {nameof(DynamoDBEvent.Version)} >= :v",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
                    {":sId",id.ToString()},
                    {":v",  0}
                }
            };


            var query = context.FromQueryAsync<DynamoDBEvent>(new QueryOperationConfig()
            {
                ConsistentRead = true,
                KeyExpression = expression,
                Limit = 1,
                BackwardSearch = true
            });

            var events = await query.GetNextSetAsync();

            if (events.Count > 0)
            {
                var firstEvent = events.FirstOrDefault();

                category = firstEvent.Category;
                version = firstEvent.Version;
            }

            if (category == null)
            {
                if (type == null)
                {
                    throw new Exception(
                        $"Attempt to get stateless instance of a non-constructable aggregate with stream: {id}");
                }

                aggregate = (IAggregate)Activator.CreateInstance(type, true);
            }
            else
            {
                aggregate = (IAggregate)Activator.CreateInstance(eventTypeLookupStrategy.Resolve(category));
            }
            ((AggregateBase)aggregate).SetState(id, version ?? 0);

            return aggregate;
        }

        private TransactWriteItem GetDynamoDbTransactItem(IAggregate aggregate, int version, IEventData eventData)
        {
            var when = DateTimeOffset.Now;
            // TimeSpan t = _when.UtcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var whenTimeStamp = (ulong)when.ToUnixTimeMilliseconds();

            var item = new TransactWriteItem
            {
                Put = new Put
                {
                    ConditionExpression = $"attribute_not_exists({nameof(DynamoDBEvent.Version)})",
                    TableName = TableConstants.EVENT_TABLE_NAME,

                    Item = new Dictionary<string, AttributeValue> {
                        { nameof(DynamoDBEvent.StreamId), new AttributeValue(aggregate.Id.ToString()) },
                        { nameof(DynamoDBEvent.Version), new AttributeValue
                            {
                                N = (version).ToString()
                            }
                        },
                        {nameof(DynamoDBEvent.CommmitedAt), new AttributeValue
                            {
                                N = whenTimeStamp.ToString()
                            }
                        },
                        {nameof(DynamoDBEvent.TransactionId), new AttributeValue(commandContext.Transaction.Id.ToString()) },
                        {nameof(DynamoDBEvent.AppVersion),  new AttributeValue(commandContext.AppVersion)},
                        {nameof(DynamoDBEvent.When), new AttributeValue(when.ToString("o"))},
                        {nameof(DynamoDBEvent.Body),  new AttributeValue(JsonConvert.SerializeObject(eventData.Event, SerializerSettings))},
                        {nameof(DynamoDBEvent.Category), new AttributeValue(aggregate.GetType().FullName)},
                        {nameof(DynamoDBEvent.BodyType), new AttributeValue(eventData.Type.FullName)},
                        {nameof(DynamoDBEvent.Who), new AttributeValue( (commandContext.ImpersonatorBy?.GuidId ?? commandContext.By.GuidId).ToString())},
                    }
                }
            };

            return item;
        }

        public async Task<IAggregateCommit> SaveAsync(IAggregate aggregate)
        {
            if(aggregate.Id == Guid.Empty)
            {
                throw new Exception(
                    $"The aggregate {aggregate.GetType().FullName} has tried to be saved with an empty id");
            }

            var uncommittedEvents = aggregate.GetUncommittedEvents().Cast<IEventData>().ToArray();
            var count = 0;

            try
            {

                while (count < uncommittedEvents.Length)
                {
                    var items = new List<TransactWriteItem>();

                    foreach (var eventData in uncommittedEvents.Skip(count).Take(10))
                    {
                        //Todo: check actual event version, as it should be correct
                        int eventVersion = (aggregate.Version - uncommittedEvents.Length + count + 1);
                        items.Add(GetDynamoDbTransactItem(aggregate, eventVersion, eventData));
                        count++;
                    }


                    var abc = await dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
                    {
                        TransactItems = items
                    });

                }
            }
            catch (Exception ex)
            {
                throw new Exception($"The aggregate {aggregate.GetType().FullName} has tried to save events to an old version of an aggregate", ex);
            }

            aggregate.ClearUncommittedEvents();
            return new AggregateCommit(aggregate.Id, commandContext.By.GuidId, uncommittedEvents);
        }

        public async Task<IEnumerable<IAggregateCommit>> ReadAsync(long from = 0, long to = 0)
        {
            var expression = new Expression()
            {
                ExpressionStatement = $"{nameof(Version)} >= :v AND {nameof(DynamoDBEvent.CommmitedAt)} >= :cmit",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
                    {":cmit",  from > 0 ? from : 0},
                    {":v",  0}
                }
            };

            if (to < from) throw new ArgumentException($"{nameof(to)} is less than the value of {nameof(from)}");


            if (to > 0)
            {
                expression.ExpressionStatement += $" AND {nameof(DynamoDBEvent.CommmitedAt)} <= :cmitTo";
                expression.ExpressionAttributeValues.Add(":cmitTo", to);
            }

            var scan = context.FromScanAsync<DynamoDBEvent>(new ScanOperationConfig
            {
                FilterExpression = expression
            });



            // var query = _context.FromQueryAsync<DynamoDBEvent>(new QueryOperationConfig()
            // {
            //     IndexName = "CommitedAt-Version-Index",
            //     KeyExpression = expression
            // });

            return ReadToAggregateCommits(await scan.GetRemainingAsync());
        }

        private IEventData ReadToIEventData(Guid streamId, DynamoDBEvent row)
        {

            var type = eventTypeLookupStrategy.Resolve(row.BodyType);
            var @event = (IEvent)JsonConvert.DeserializeObject(row.Body, type);
            //@event.StreamId = streamId;

            var when = row.When;
            var version = row.Version;

            var eventData = (IEventData)new EventData(type, @event, when.UtcDateTime, version);
            return eventData;
        }

        private IEnumerable<IAggregateCommit> ReadToAggregateCommits(IEnumerable<DynamoDBEvent> rows)
        {

            foreach (var row in rows)
            {
                var streamId = row.StreamId;
                var who = row.Who;
                var eventData = ReadToIEventData(streamId, row);
                yield return new AggregateCommit(streamId, who, new[] { eventData });
            }

        }

        public async Task<IEnumerable<IAggregateCommit>> ReadAsync(Guid streamId)
        {
            var expression = new Expression()
            {
                ExpressionStatement = $"{nameof(DynamoDBEvent.StreamId)} = :sId AND {nameof(DynamoDBEvent.Version)} >= :v",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
                    {":sId",streamId.ToString()},
                    {":v",  0}
                }
            };

            var query = context.FromQueryAsync<DynamoDBEvent>(new QueryOperationConfig()
            {
                ConsistentRead = true,
                KeyExpression = expression
            });

            var events = await query.GetRemainingAsync();



            return ReadToAggregateCommits(events);

        }
        public async Task<IEnumerable<IAggregateCommit>> ReadNewestLimitAsync(Guid streamId, int limit = 50)
        {
            var expression = new Expression()
            {
                ExpressionStatement = $"{nameof(DynamoDBEvent.StreamId)} = :sId  AND {nameof(DynamoDBEvent.Version)} >= :v",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
                    {":sId",streamId.ToString()},
                    {":v",  0}
                }
            };

            var query = context.FromQueryAsync<DynamoDBEvent>(new QueryOperationConfig()
            {
                ConsistentRead = true,
                KeyExpression = expression,
                Limit = limit,
                BackwardSearch = true
            });

            var events = await query.GetRemainingAsync();
            return ReadToAggregateCommits(events);

        }

        public async Task<TAggregate> GetVersionAsync<TAggregate>(Guid id, long version) where TAggregate : IAggregate
        {
            IAggregate aggregate = await GetAsync(typeof(TAggregate), id, version);
            return (TAggregate)aggregate;
        }
    }
}