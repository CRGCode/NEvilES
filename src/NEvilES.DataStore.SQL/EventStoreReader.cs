using System;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.SQL
{
    public class EventStoreReader : IReadEventStore
    {
        protected readonly IDbTransaction Transaction;
        protected readonly IEventTypeLookupStrategy EventTypeLookupStrategy;

        public EventStoreReader(IDbTransaction transaction, IEventTypeLookupStrategy eventTypeLookupStrategy)
        {
            Transaction = transaction;
            EventTypeLookupStrategy = eventTypeLookupStrategy;
        }

        protected static IDbDataParameter CreateParam(IDbCommand cmd, string name, DbType type, object value = null)
        {
            return CreateParam(cmd, name, type, null, value);
        }

        protected static IDbDataParameter CreateParam(IDbCommand cmd, string name, DbType type, int? size,
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
            using (var cmd = Transaction.Connection.CreateCommand())
            {
                cmd.Transaction = Transaction;
                cmd.CommandType = CommandType.Text;
                if (from == 0 && to == 0)
                {
                    cmd.CommandText =
                        "SELECT streamid, metadata, bodytype, body, who, _when, version FROM events ORDER BY id";
                }
                else
                {
                    cmd.CommandText =
                        "SELECT streamid, metadata, bodytype, body, who, _when, version FROM events WHERE id BETWEEN @from AND @to ORDER BY id";
                    CreateParam(cmd, "@from", DbType.Int64, from);
                    CreateParam(cmd, "@to", DbType.Int64, to);
                }

                return ReadToAggregateCommits(cmd);
            }
        }

        protected IEnumerable<IAggregateCommit> ReadToAggregateCommits(IDbCommand cmd)
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

        protected IEventData ReadToIEventData(Guid streamId, IDataReader reader)
        {

            var type = EventTypeLookupStrategy.Resolve(reader.GetString(2));
            var @event = (IEvent)JsonConvert.DeserializeObject(reader.GetString(3), type);
            @event.StreamId = streamId;

            var when = reader.GetDateTime(5);
            var version = reader.GetInt32(6);

            var eventData = (IEventData)new EventData(type, @event, when, version);
            return eventData;
        }

        public IEnumerable<IAggregateCommit> Read(Guid streamId)
        {
            using (var cmd = Transaction.Connection.CreateCommand())
            {
                cmd.Transaction = Transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    "SELECT streamid, metadata, bodytype, body, who, _when, version FROM events WHERE streamid = @streamid order by id";
                CreateParam(cmd, "@streamid", DbType.Guid, streamId);

                return ReadToAggregateCommits(cmd);
            }
        }

        public IEnumerable<IAggregateCommit> ReadNewestLimit(Guid streamId, int limit = 50)
        {
            using (var cmd = Transaction.Connection.CreateCommand())
            {
                cmd.Transaction = Transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    "SELECT streamid, metadata, bodytype, body, who, _when, version FROM events WHERE streamid = @streamid order by id DESC limit @limit";
                CreateParam(cmd, "@streamid", DbType.Guid, streamId);
                CreateParam(cmd, "@limit", DbType.Int32, null, limit);

                return ReadToAggregateCommits(cmd);
            }
        }

    }
}