using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NEvilES.Pipeline;

namespace NEvilES.DataStore
{
    public class DatabaseEventStore : IRepository
    {
        private readonly IDbTransaction _transaction;
        private readonly IEventTypeLookupStrategy _eventTypeLookupStrategy;
        private readonly CommandContext _commandContext;

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new JsonConverter[] { new StringEnumConverter() }
        };

        public DatabaseEventStore(IDbTransaction transaction, IEventTypeLookupStrategy eventTypeLookupStrategy, CommandContext commandContext)
        {
            _transaction = transaction;
            _eventTypeLookupStrategy = eventTypeLookupStrategy;
            _commandContext = commandContext;
        }

        public TAggregate Get<TAggregate>(Guid id) where TAggregate : IAggregate
        {
            return (TAggregate)Get(typeof(TAggregate), id);
        }

        public IAggregate Get(Type type, Guid id)
        {
            var events = new List<EventDb>();
            using (var cmd = _transaction.Connection.CreateCommand())
            {
                cmd.Transaction = _transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    @"SELECT Id, Category, StreamId, TransactionId, MetaData, BodyType, Body, [By], [At], [Version], AppVersion, SessionId FROM Events WHERE StreamId=@StreamId ORDER BY Id";
                CreateParam(cmd, "@StreamId", DbType.Guid, id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var item = new EventDb
                        {
                            Id = reader.GetInt32(0),
                            Category = reader.GetString(1),
                            StreamId = reader.GetGuid(2),
                            TransactionId = reader.GetGuid(3),
                            Metadata = reader.GetString(4),
                            BodyType = reader.GetString(5),
                            By = reader.GetGuid(6),
                            At = reader.GetDateTime(7),
                            Version = reader.GetInt32(8),
                            AppVersion = reader.GetString(9),
                            SessionId = reader.GetGuid(10)
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

            var aggregate = (IAggregate)Activator.CreateInstance(_eventTypeLookupStrategy.Resolve(events[0].Category));

            foreach (var eventDb in events.OrderBy(x => x.Version))
            {
                var message = (IEvent)JsonConvert.DeserializeObject(eventDb.Body, _eventTypeLookupStrategy.Resolve(eventDb.BodyType));
                message.StreamId = eventDb.StreamId;
                aggregate.ApplyEvent(message);
            }
            ((AggregateBase)aggregate).SetState(id);

            return aggregate;
        }

        public IAggregate GetStateless(Type type, Guid id)
        {
            IAggregate aggregate;

            int? version;
            string category;
            using (var cmd = _transaction.Connection.CreateCommand())
            {
                cmd.Transaction = _transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    @"SELECT [Version], Category FROM Events WHERE StreamId=@StreamId ORDER BY Id DESC";
                CreateParam(cmd, "@StreamId", DbType.Guid, id);

                using (var reader = cmd.ExecuteReader())
                {
                    version = reader.GetInt32(0);
                    category = reader.GetString(1);
                }
            }

            if (category == null)
            {
                if(type == null)
                {
                    throw new Exception($"Attempt to get stateless instance of a non-constructable aggregate with stream: {id}");
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

        public AggregateCommit Save(IAggregate aggregate)
        {
            if (aggregate.Id == Guid.Empty)
            {
                throw new Exception($"The aggregate {aggregate.GetType().FullName} has tried to be saved will an empty id");
            }

            var eventDatas = aggregate.GetUncommittedEvents().Cast<IEventData>().ToArray();
            var count = 0;

            var metadata = string.Empty;
            using (var cmd = _transaction.Connection.CreateCommand())
            {
                cmd.Transaction = _transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    @"INSERT INTO Events VALUES(@Category, @StreamId, @TransactionId, @MetaData, @BodyType, @Body, @By, @At, @Version, @AppVersion, @SessionId)";
                var category = CreateParam(cmd, "@Category", DbType.String,500);
                var streamId = CreateParam(cmd, "@StreamId", DbType.Guid);
                var version = CreateParam(cmd, "@Version", DbType.Int32);
                var transactionId = CreateParam(cmd, "@TransactionId", DbType.Guid);
                var metaData = CreateParam(cmd, "@MetaData", DbType.String,-1);
                var bodyType = CreateParam(cmd, "@BodyType", DbType.String,500);
                var body = CreateParam(cmd, "@Body", DbType.String,-1);
                var by = CreateParam(cmd, "@By", DbType.Guid);
                var at = CreateParam(cmd, "@At", DbType.DateTime);
                var appVersion = CreateParam(cmd, "@AppVersion", DbType.String,20);
                var sessionId = CreateParam(cmd, "@SessionId", DbType.Guid);
                cmd.Prepare();

                foreach (var eventData in eventDatas)
                {
                    streamId.Value = aggregate.Id;
                    version.Value = aggregate.Version - eventDatas.Length + count + 1;
                    transactionId.Value = _commandContext.TransactionId;
                    appVersion.Value = _commandContext.AppVersion;
                    at.Value = DateTime.UtcNow;
                    body.Value = JsonConvert.SerializeObject(eventData.Event, _serializerSettings);
                    category.Value = aggregate.GetType().FullName;
                    bodyType.Value = eventData.Type;
                    by.Value = _commandContext.ImpersonatorBy?.GuidId ?? _commandContext.By.GuidId;
                    sessionId.Value = _commandContext.SessionId;
                    metaData.Value = metadata;

                    cmd.ExecuteNonQuery();
                    count++;
                }
            }

            aggregate.ClearUncommittedEvents();
            return new AggregateCommit(aggregate.Id, _commandContext.By.GuidId, metadata, eventDatas);
        }

        private static IDbDataParameter CreateParam(IDbCommand cmd, string name, DbType type, object value = null)
        {
            return CreateParam(cmd, name, type, null, value);
        }

        private static IDbDataParameter CreateParam(IDbCommand cmd, string name, DbType type, int? size, object value = null)
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
    }
}
