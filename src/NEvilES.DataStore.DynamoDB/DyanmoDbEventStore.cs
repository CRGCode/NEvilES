using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using NEvilES;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Abstractions.Pipeline.Async;
using NEvilES.Pipeline;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NEvilES.DataStore.DynamoDB
{
    public class DynamoDBEventStore : IAsyncRepository, IAsyncAggregateHistory
    {
        private readonly IDynamoDBContext _context;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IEventTypeLookupStrategy _eventTypeLookupStrategy;
        private readonly ICommandContext _commandContext;
        public static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new JsonConverter[] { new StringEnumConverter() }
        };
        private string _tableName = TableConstants.EVENT_TABLE_NAME;

        public DynamoDBEventStore(
            IAmazonDynamoDB dynamoDbClient,
            IEventTypeLookupStrategy eventTypeLookupStrategy,
            ICommandContext commandContext
        )
        {
            _dynamoDbClient = dynamoDbClient;
            _context = new DynamoDBContext(_dynamoDbClient);
            _eventTypeLookupStrategy = eventTypeLookupStrategy;
            _commandContext = commandContext;

            if (AWSConfigsDynamoDB.Context.TableAliases.TryGetValue(_tableName, out string name))
            {
                _tableName = name;
            }
        }

        public virtual async Task<TAggregate> GetAsync<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            IAggregate aggregate = await GetAsync(typeof(TAggregate), id);
            return (TAggregate)aggregate;
        }
        public virtual Task<IAggregate> GetAsync(Type type, Guid id) => GetAsync(type, id, null);

        public virtual async Task<IAggregate> GetAsync(Type type, Guid id, Int64? version)
        {
            var expression = new Expression()
            {
                ExpressionStatement = $"{nameof(DynamoDBEventTable.StreamId)} = :sId",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
                    {":sId",id.ToString()},
                    {":v",  version.HasValue ? version.Value : 0}
                }
            };

            if (version.HasValue && version.Value > 0)
            {
                expression.ExpressionStatement += $" AND {nameof(DynamoDBEventTable.Version)} <= :v";
            }
            else
            {
                expression.ExpressionStatement += $" AND {nameof(DynamoDBEventTable.Version)} >= :v";
            }

            var query = _context.FromQueryAsync<DynamoDBEventTable>(new QueryOperationConfig()
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

        public virtual async Task<IAggregate> GetStatelessAsync(Type type, Guid id)
        {
            IAggregate aggregate;

            int? version = null;
            string category = null;

            var expression = new Expression()
            {
                ExpressionStatement = $"{nameof(DynamoDBEventTable.StreamId)} = :sId  AND {nameof(DynamoDBEventTable.Version)} >= :v",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
                    {":sId",id.ToString()},
                    {":v",  0}
                }
            };


            var query = _context.FromQueryAsync<DynamoDBEventTable>(new QueryOperationConfig()
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
                aggregate = (IAggregate)Activator.CreateInstance(_eventTypeLookupStrategy.Resolve(category));
            }
            ((AggregateBase)aggregate).SetState(id, version ?? 0);

            return aggregate;
        }

        private TransactWriteItem GetDynamoDbTransactItem(IAggregate aggregate, int version, string metadata, IEventData eventData)
        {


            var _when = DateTimeOffset.Now;
            // TimeSpan t = _when.UtcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var _whenTimeStamp = (ulong)_when.ToUnixTimeMilliseconds();

            var item = new Amazon.DynamoDBv2.Model.TransactWriteItem
            {
                Put = new Amazon.DynamoDBv2.Model.Put
                {
                    ConditionExpression = $"attribute_not_exists({nameof(DynamoDBEventTable.Version)})",
                    TableName = _tableName,

                    Item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> {
                        { nameof(DynamoDBEventTable.StreamId), new AttributeValue(aggregate.Id.ToString()) },
                        { nameof(DynamoDBEventTable.Version), new AttributeValue
                            {
                                N = (version).ToString()
                            }
                        },
                        {nameof(DynamoDBEventTable.CommmitedAt), new AttributeValue
                            {
                                N = _whenTimeStamp.ToString()
                            }
                        },
                        {nameof(DynamoDBEventTable.TransactionId), new AttributeValue(_commandContext.Transaction.Id.ToString()) },
                        {nameof(DynamoDBEventTable.AppVersion),  new AttributeValue(_commandContext.AppVersion)},
                        {nameof(DynamoDBEventTable.When), new AttributeValue(_when.ToString("o"))},
                        {nameof(DynamoDBEventTable.Body),  new AttributeValue(JsonConvert.SerializeObject(eventData.Event, SerializerSettings))},
                        {nameof(DynamoDBEventTable.Category), new AttributeValue(aggregate.GetType().FullName)},
                        {nameof(DynamoDBEventTable.BodyType), new AttributeValue(eventData.Type.FullName)},
                        {nameof(DynamoDBEventTable.Who), new AttributeValue( (_commandContext.ImpersonatorBy?.GuidId ?? _commandContext.By.GuidId).ToString())},
                        // {"metaData", new AttributeValue(metadata)},
                    }
                }
            };

            return item;
        }

        public virtual async Task<IAggregateCommit> SaveAsync(IAggregate aggregate)
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

                while (count < uncommittedEvents.Length)
                {
                    var items = new List<TransactWriteItem>();

                    foreach (var eventData in uncommittedEvents.Skip(count).Take(10))
                    {
                        //Todo: check actual event version, as it should be correct
                        int eventVersion = (aggregate.Version - uncommittedEvents.Length + count + 1);
                        items.Add(GetDynamoDbTransactItem(aggregate, eventVersion, metadata, eventData));
                        count++;
                    }


                    var abc = await _dynamoDbClient.TransactWriteItemsAsync(new Amazon.DynamoDBv2.Model.TransactWriteItemsRequest
                    {
                        TransactItems = items
                    });

                }
            }
            catch (AmazonDynamoDBException)
            {
                throw new AggregateOutOfDate((AggregateBase)aggregate,
                                   $"The aggregate {aggregate.GetType().FullName} has tried to save events to an old version of an aggregate");
            }
            catch (Exception e)
            {
                throw new Exception($"The aggregate {aggregate.GetType().FullName} has tried to save events to an old version of an aggregate");
            }

            aggregate.ClearUncommittedEvents();
            return new AggregateCommit(aggregate.Id, _commandContext.By.GuidId, metadata, uncommittedEvents);
        }






        public async Task<IEnumerable<IAggregateCommit>> ReadAsync(long from = 0, long to = 0)
        {
            var expression = new Expression()
            {
                ExpressionStatement = $"{nameof(Version)} >= :v AND {nameof(DynamoDBEventTable.CommmitedAt)} >= :cmit",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
                    {":cmit",  from > 0 ? from : 0},
                    {":v",  0}
                }
            };

            if (to < from) throw new ArgumentException($"{nameof(to)} is less than the value of {nameof(from)}");


            if (to > 0)
            {
                expression.ExpressionStatement += $" AND {nameof(DynamoDBEventTable.CommmitedAt)} <= :cmitTo";
                expression.ExpressionAttributeValues.Add(":cmitTo", to);
            }

            var scan = _context.FromScanAsync<DynamoDBEventTable>(new ScanOperationConfig
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

        private IEventData ReadToIEventData(Guid streamId, DynamoDBEventTable row)
        {

            var type = _eventTypeLookupStrategy.Resolve(row.BodyType);
            var @event = (IEvent)JsonConvert.DeserializeObject(row.Body, type);
            @event.StreamId = streamId;

            var when = row.When;
            var version = row.Version;

            var eventData = (IEventData)new EventData(type, @event, when.UtcDateTime, version);
            return eventData;
        }

        private IEnumerable<IAggregateCommit> ReadToAggregateCommits(IEnumerable<DynamoDBEventTable> rows)
        {

            foreach (var row in rows)
            {
                var streamId = row.StreamId;
                var who = row.Who;
                var eventData = ReadToIEventData(streamId, row);
                yield return new AggregateCommit(streamId, who, "", new[] { eventData });
            }

        }

        public virtual async Task<IEnumerable<IAggregateCommit>> ReadAsync(Guid streamId)
        {
            var expression = new Expression()
            {
                ExpressionStatement = $"{nameof(DynamoDBEventTable.StreamId)} = :sId AND {nameof(DynamoDBEventTable.Version)} >= :v",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
                    {":sId",streamId.ToString()},
                    {":v",  0}
                }
            };

            var query = _context.FromQueryAsync<DynamoDBEventTable>(new QueryOperationConfig()
            {
                ConsistentRead = true,
                KeyExpression = expression
            });

            var events = await query.GetRemainingAsync();



            return ReadToAggregateCommits(events);

        }
        public virtual async Task<IEnumerable<IAggregateCommit>> ReadNewestLimitAsync(Guid streamId, int limit = 50)
        {
            var expression = new Expression()
            {
                ExpressionStatement = $"{nameof(DynamoDBEventTable.StreamId)} = :sId  AND {nameof(DynamoDBEventTable.Version)} >= :v",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
                    {":sId",streamId.ToString()},
                    {":v",  0}
                }
            };

            var query = _context.FromQueryAsync<DynamoDBEventTable>(new QueryOperationConfig()
            {
                ConsistentRead = true,
                KeyExpression = expression,
                Limit = limit,
                BackwardSearch = true
            });

            var events = await query.GetRemainingAsync();
            return ReadToAggregateCommits(events);

        }

        public virtual async Task<TAggregate> GetVersionAsync<TAggregate>(Guid id, Int64 version) where TAggregate : IAggregate
        {
            IAggregate aggregate = await GetAsync(typeof(TAggregate), id, version);
            return (TAggregate)aggregate;
        }
    }
}