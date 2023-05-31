using System;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.SQL
{
    public class SQLEventStoreReader : IReadEventStore 
    {
        public readonly IDbTransaction Transaction;
        protected readonly IEventTypeLookupStrategy EventTypeLookupStrategy;

        public SQLEventStoreReader(IDbTransaction transaction, IEventTypeLookupStrategy eventTypeLookupStrategy)
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

        public IEnumerable<IAggregateCommit> Read(long from = 0, long to = 0)
        {
            using var cmd = Transaction.Connection!.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandType = CommandType.Text;
            if (from == 0 && to == 0)
            {
                cmd.CommandText =
                    "SELECT streamid, bodytype, body, who, _when, version FROM events ORDER BY id";
            }
            else
            {
                cmd.CommandText =
                    "SELECT streamid, bodytype, body, who, _when, version FROM events WHERE id BETWEEN @from AND @to ORDER BY id";
                CreateParam(cmd, "@from", DbType.Int64, from);
                CreateParam(cmd, "@to", DbType.Int64, to);
            }

            foreach (var aggregateCommit in AggregateCommits(cmd))
            {
                yield return aggregateCommit;
            }
        }

        public IEnumerable<IAggregateCommit> ReadNewestLimit(int limit = 50)
        {
            using var cmd = Transaction.Connection!.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT streamid, bodytype, body, who, _when, version FROM events ORDER BY id DESC";
            foreach (var aggregateCommit in AggregateCommits(cmd))
            {
                yield return aggregateCommit;
                if(limit-- ==0 ) { break; }  // Force limit - Note Can't use SQL TOP as MySql uses LIMIT
            }
        }

        public IEnumerable<IAggregateCommit> Read(Guid streamId)
        {
            using var cmd = Transaction.Connection!.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText =
                "SELECT streamid, bodytype, body, who, _when, version FROM events WHERE streamid = @streamId order by id";
            CreateParam(cmd, "@streamId", DbType.Guid, streamId);

            foreach (var aggregateCommit in AggregateCommits(cmd)) yield return aggregateCommit;
        }

        public IEnumerable<IAggregateCommit> ReadNewestLimit(Guid streamId, int limit = 50)
        {
            using var cmd = Transaction.Connection!.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText =
                "SELECT streamid, bodytype, body, who, _when, version FROM events WHERE streamid = @streamId order by id DESC limit @limit";
            CreateParam(cmd, "@streamId", DbType.Guid, streamId);
            CreateParam(cmd, "@limit", DbType.Int32, null, limit);

            foreach (var aggregateCommit in AggregateCommits(cmd)) yield return aggregateCommit;
        }

        private IEnumerable<IAggregateCommit> AggregateCommits(IDbCommand cmd)
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var streamId = reader.GetGuid(0);
                var type = EventTypeLookupStrategy.Resolve(reader.GetString(1));
                var @event = (IEvent)JsonConvert.DeserializeObject(reader.GetString(2), type);
                //@event.StreamId = streamId;
                var who = reader.GetGuid(3);
                var when = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc);
                var version = reader.GetInt32(5);

                var eventData = (IEventData)new EventData(type, @event, when, version);

                yield return new AggregateCommit(streamId, who, new[] { eventData });
            }
        }
    }
}