using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.SQL
{
    public class SQLEventStore : SQLEventStoreReader, IRepository
    {

        private readonly ICommandContext commandContext;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new JsonConverter[] { new StringEnumConverter() }
        };

        public SQLEventStore(IDbTransaction transaction, IEventTypeLookupStrategy eventTypeLookupStrategy,
            ICommandContext commandContext) : base(transaction, eventTypeLookupStrategy)
        {
            this.commandContext = commandContext;
        }

        public TAggregate Get<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            return (TAggregate)Get(typeof(TAggregate), id);
        }

        public IAggregate Get(Type type, Guid id) => Get(type, id, null);

        public IAggregate Get(Type type, Guid id, long? version)
        {
            var events = new List<EventDb>();
            using (var cmd = Transaction.Connection.CreateCommand())
            {
                cmd.Transaction = Transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    @"SELECT id, category, streamid, transactionid, bodytype, body, who, _when
, version, appversion FROM events WHERE streamid=@StreamId";
                if (version.HasValue)
                {
                    cmd.CommandText += " and version <= @Version";
                    CreateParam(cmd, "@Version", DbType.Int64, version);
                }

                cmd.CommandText += " ORDER BY id";
                CreateParam(cmd, "@StreamId", DbType.Guid, id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ord = 0;
                        var item = new EventDb
                        {
                            Id = reader.GetInt64(ord++),
                            Category = reader.GetString(ord++),
                            StreamId = reader.GetGuid(ord++),
                            TransactionId = reader.GetGuid(ord++),
                            BodyType = reader.GetString(ord++),
                            Body = reader.GetString(ord++),
                            Who = reader.GetGuid(ord++),
                            When = reader.GetDateTime(ord++),
                            Version = reader.GetInt32(ord++),
                            AppVersion = reader.GetString(ord),
                        };
                        events.Add(item);
                    }
                }
            }

            if (events.Count == 0)
            {
                var emptyAggregate = (IAggregate)Activator.CreateInstance(type, true);
                ((AggregateBase)emptyAggregate).SetState(id);
                return emptyAggregate;
            }

            var aggregate = (IAggregate)Activator.CreateInstance(EventTypeLookupStrategy.Resolve(events[0].Category));

            foreach (var eventDb in events.OrderBy(x => x.Version))
            {
                var message =
                    (IEvent)
                    JsonConvert.DeserializeObject(eventDb.Body, EventTypeLookupStrategy.Resolve(eventDb.BodyType),
                        SerializerSettings);
                message.StreamId = eventDb.StreamId;
                aggregate.ApplyEvent(message);
            }

            ((AggregateBase)aggregate).SetState(id);

            return aggregate;
        }

        public IAggregate GetStateless(Type type, Guid id)
        {
            IAggregate aggregate;

            int? version = null;
            string category = null;
            using (var cmd = Transaction.Connection.CreateCommand())
            {
                cmd.Transaction = Transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    @"SELECT version, category FROM events WHERE StreamId=@StreamId ORDER BY id DESC";
                CreateParam(cmd, "@StreamId", DbType.Guid, id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        version = reader.GetInt32(0);
                        category = reader.GetString(1);
                    }
                }
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
                aggregate = (IAggregate)Activator.CreateInstance(EventTypeLookupStrategy.Resolve(category));
            }

            ((AggregateBase)aggregate).SetState(id, version ?? 0);

            return aggregate;
        }

        public TAggregate GetVersion<TAggregate>(Guid id, long version) where TAggregate : IAggregate
        {
            return (TAggregate)Get(typeof(TAggregate), id, version);
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

            using (var cmd = Transaction.Connection.CreateCommand())
            {
                cmd.Transaction = Transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    @"INSERT INTO events(category,streamid,transactionid,bodytype,body,who,_when,version,appversion)
                    VALUES(@Category, @StreamId, @TransactionId, @BodyType, @Body, @Who, @When, @Version, @AppVersion)";
                var category = CreateParam(cmd, "@Category", DbType.String, 500);
                var streamId = CreateParam(cmd, "@StreamId", DbType.Guid);
                var version = CreateParam(cmd, "@Version", DbType.Int32);
                var transactionId = CreateParam(cmd, "@TransactionId", DbType.Guid);
                var bodyType = CreateParam(cmd, "@BodyType", DbType.String, 500);
                var body = CreateParam(cmd, "@Body", DbType.String, -1);
                var by = CreateParam(cmd, "@Who", DbType.Guid);
                var at = CreateParam(cmd, "@When", DbType.DateTime);
                var appVersion = CreateParam(cmd, "@AppVersion", DbType.String, 20);
                cmd.Prepare();

                foreach (var eventData in uncommittedEvents)
                {
                    streamId.Value = aggregate.Id;
                    version.Value = aggregate.Version - uncommittedEvents.Length + count + 1;
                    transactionId.Value = commandContext.Transaction.Id;
                    appVersion.Value = commandContext.AppVersion;
                    at.Value = DateTime.UtcNow;
                    body.Value = JsonConvert.SerializeObject(eventData.Event, SerializerSettings);
                    category.Value = aggregate.GetType().FullName;
                    bodyType.Value = eventData.Type.FullName;
                    by.Value = commandContext.ImpersonatorBy?.GuidId ?? commandContext.By.GuidId;

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        if (e.Message.ToLower().Contains("unique"))
                            throw new AggregateConcurrencyException(aggregate.Id, eventData);
                        throw;
                    }
                    count++;
                }
            }

            aggregate.ClearUncommittedEvents();
            return new AggregateCommit(aggregate.Id, commandContext.By.GuidId, uncommittedEvents);
        }
    }
}