using System;
using System.Collections.Generic;
using System.Data;
using Outbox.Abstractions;

namespace NEvilES.DataStore.SQL.Tests
{
    public class SQLOutboxRepository : IOutboxRepository
    {
        private readonly IDbTransaction transaction;

        public SQLOutboxRepository(SQLEventStoreBase eventStore)
        {
            transaction = eventStore.Transaction;
        }

        protected static IDbDataParameter CreateParam(IDbCommand cmd, string name, DbType type, object value = null, int? size = null)
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

        public void Add(OutboxMessage message)
        {
            using var cmd = transaction.Connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "INSERT INTO outbox(messageid, messagetype, payload, destination) VALUES(@messageId, @messageType, @payload, @destination)";
            var messageId = CreateParam(cmd, "@messageId", DbType.Guid, message.MessageId);
            var messageType = CreateParam(cmd, "@messageType", DbType.String, message.MessageType);
            var payload = CreateParam(cmd, "@payload", DbType.String, message.Payload);
            var destination = CreateParam(cmd, "@destination", DbType.String, message.Destination);

            //cmd.Prepare();

            cmd.ExecuteNonQuery();
        }

        public IEnumerable<OutboxMessage> GetNext()
        {
            using var cmd = transaction.Connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT id, messageid, messagetype, payload, destination, createdat FROM outbox ORDER BY id";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var ord = 0;
                yield return new OutboxMessage()
                {
                    Id = reader.GetInt32(ord++),
                    MessageId = reader.GetGuid(ord++),
                    MessageType = reader.GetString(ord++),
                    Payload = reader.GetString(ord++),
                    Destination = reader.GetString(ord++),
                    CreatedAt = reader.GetDateTime(ord)
                };
            }
        }

        public void Remove(int messageId)
        {
            throw new System.NotImplementedException();
        }
    }
}