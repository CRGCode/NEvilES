using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NEvilES.Pipeline;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.SQL
{
    public class DatabaseEventStore : IRepository, IAggregateHistory
    {
        private readonly IDbTransaction transaction;
        private readonly IEventTypeLookupStrategy eventTypeLookupStrategy;
        private readonly CommandContext commandContext;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new JsonConverter[] { new StringEnumConverter() }
        };

        public DatabaseEventStore(IDbTransaction transaction, IEventTypeLookupStrategy eventTypeLookupStrategy,
            CommandContext commandContext)
        {
            this.transaction = transaction;
            this.eventTypeLookupStrategy = eventTypeLookupStrategy;
            this.commandContext = commandContext;
        }

        public TAggregate Get<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            return (TAggregate)Get(typeof(TAggregate), id);
        }

        public IAggregate Get(Type type, Guid id) => Get(type, id, null);
        public IAggregate Get(Type type, Guid id, Int64? version)
        {
            var events = new List<EventDb>();
            using (var cmd = transaction.Connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    @"SELECT id, category, streamid, transactionid, metadata, bodytype, body, who, _when
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
                        var item = new EventDb
                        {
                            Id = reader.GetInt64(0),
                            Category = reader.GetString(1),
                            StreamId = reader.GetGuid(2),
                            TransactionId = reader.GetGuid(3),
                            // Metadata = "",
                            BodyType = reader.GetString(5),
                            Body = reader.GetString(6),
                            Who = reader.GetGuid(7),
                            When = reader.GetDateTime(8),
                            Version = reader.GetInt32(9),
                            AppVersion = reader.GetString(10),
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

            var aggregate = (IAggregate)Activator.CreateInstance(eventTypeLookupStrategy.Resolve(events[0].Category));

            foreach (var eventDb in events.OrderBy(x => x.Version))
            {
                var message =
                    (IEvent)
                    JsonConvert.DeserializeObject(eventDb.Body, eventTypeLookupStrategy.Resolve(eventDb.BodyType), SerializerSettings);
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
            using (var cmd = transaction.Connection.CreateCommand())
            {
                cmd.Transaction = transaction;
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
                aggregate = (IAggregate)Activator.CreateInstance(eventTypeLookupStrategy.Resolve(category));
            }
            ((AggregateBase)aggregate).SetState(id, version ?? 0);

            return aggregate;
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
            using (var cmd = transaction.Connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    @"INSERT INTO events(category,streamid,transactionid,metadata,bodytype,body,who,_when,version,appversion)
                    VALUES(@Category, @StreamId, @TransactionId, @MetaData, @BodyType, @Body, @Who, @When, @Version, @AppVersion)";
                var category = CreateParam(cmd, "@Category", DbType.String, 500);
                var streamId = CreateParam(cmd, "@StreamId", DbType.Guid);
                var version = CreateParam(cmd, "@Version", DbType.Int32);
                var transactionId = CreateParam(cmd, "@TransactionId", DbType.Guid);
                var metaData = CreateParam(cmd, "@MetaData", DbType.String, -1);
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
                    metaData.Value = "";

                    cmd.ExecuteNonQuery();
                    count++;
                }
            }

            aggregate.ClearUncommittedEvents();
            return new AggregateCommit(aggregate.Id, commandContext.By.GuidId, metadata, uncommittedEvents);
        }

        private static IDbDataParameter CreateParam(IDbCommand cmd, string name, DbType type, object value = null)
        {
            return CreateParam(cmd, name, type, null, value);
        }

        private static IDbDataParameter CreateParam(IDbCommand cmd, string name, DbType type, int? size,
            object value = null)
        {
            var param = cmd.CreateParameter();
            param.DbType = type;
            param.ParameterName = name;
            if (size.HasValue)
                param.Size = size.Value;
            if (value != null)
                param.Value = value;
            cmd.Parameters.Add(param);
            return param;
        }

        public IEnumerable<IAggregateCommit> Read(Int64 from = 0, Int64 to = 0)
        {
            using (var cmd = transaction.Connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                if (from == 0 && to == 0)
                {
                    cmd.CommandText = "SELECT streamid, metadata, bodytype, body, who, _when, version FROM events ORDER BY id";
                }
                else
                {
                    cmd.CommandText = "SELECT streamid, metadata, bodytype, body, who, _when, version FROM events WHERE id BETWEEN @from AND @to ORDER BY id";
                    CreateParam(cmd, "@from", DbType.Int64, from);
                    CreateParam(cmd, "@to", DbType.Int64, to);
                }

                return ReadToAggregateCommits(cmd);
            }
        }

        private IEventData ReadToIEventData(Guid streamId, IDataReader reader)
        {

            var type = eventTypeLookupStrategy.Resolve(reader.GetString(2));
            var @event = (IEvent)JsonConvert.DeserializeObject(reader.GetString(3), type); @event.StreamId = streamId;

            var when = reader.GetDateTime(5);
            var version = reader.GetInt32(6);

            var eventData = (IEventData)new EventData(type, @event, when, version);
            return eventData;
        }

        private IEnumerable<IAggregateCommit> ReadToAggregateCommits(IDbCommand cmd)
        {
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var streamId = reader.GetGuid(0);
                    var metadata = reader.GetString(1);
                    var who = reader.GetGuid(4);

                    var eventData = ReadToIEventData(streamId, reader);
                    yield return new AggregateCommit(streamId, who, metadata, new[] { eventData });
                }
            }
        }

        public IEnumerable<IAggregateCommit> Read(Guid streamId)
        {
            using (var cmd = transaction.Connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT streamid, metadata, bodytype, body, who, _when, version FROM events WHERE streamid = @streamid order by id";
                CreateParam(cmd, "@streamid", DbType.Guid, streamId);

                return ReadToAggregateCommits(cmd);
            }
        }
        public IEnumerable<IAggregateCommit> ReadNewestLimit(Guid streamId, int limit = 50)
        {
            using (var cmd = transaction.Connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT streamid, metadata, bodytype, body, who, _when, version FROM events WHERE streamid = @streamid order by id DESC limit @limit";
                CreateParam(cmd, "@streamid", DbType.Guid, streamId);
                CreateParam(cmd, "@limit", DbType.Int32, null, limit);

                return ReadToAggregateCommits(cmd);
            }
        }

        public TAggregate GetVersion<TAggregate>(Guid id, Int64 version) where TAggregate : IAggregate
        {
            return (TAggregate)Get(typeof(TAggregate), id, version);
        }
    }
}