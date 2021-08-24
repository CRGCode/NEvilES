using System;
using System.Collections.Generic;
using System.Data;
using NEvilES.Abstractions.Pipeline;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NEvilES.DataStore.SQL
{
    public class SQLDocumentRepository : IReadFromReadModel, IWriteReadModel
    {
        private readonly IDbTransaction transaction;
        private readonly HashSet<string> docTypes;

        public SQLDocumentRepository(IDbTransaction transaction)
        {
            this.transaction = transaction;
            docTypes = new HashSet<string>();
        }

        public void Insert<T>(T item) where T : class, IHaveIdentity
        {
            var docName = CheckDocTypeExists<T>();

            var connection = transaction.Connection;

            object json = JsonConvert.SerializeObject(item);
            var sql = $"insert into Doc.{docName} values ('{item.Id}','{json}')";

            var command = CreateCommand<T>(connection, sql);

            command.ExecuteNonQuery();
        }

        public void Update<T>(T item) where T : class, IHaveIdentity
        {
            var docName = CheckDocTypeExists<T>();

            var connection = transaction.Connection;

            var json = JsonConvert.SerializeObject(item);
            var sql = $"update Doc.{docName} set Data = '{json}' where Id = '{item.Id}'";

            var command = CreateCommand<T>(connection, sql);

            command.ExecuteNonQuery();
        }

        public void Save<T>(T item) where T : class, IHaveIdentity
        {
            var docName = CheckDocTypeExists<T>();

            var connection = transaction.Connection;

            var json = JsonConvert.SerializeObject(item);
            var sql = @$"
IF EXISTS (SELECT 1 FROM Doc.{docName} WHERE Id = '{item.Id}')
	update Doc.{docName} set Data = 'xxx' where Id = '{item.Id}'
ELSE
	insert into Doc.{docName} values ('{item.Id}','{json}')";

            var command = CreateCommand<T>(connection, sql);

            command.ExecuteNonQuery();
        }

        public void Delete<T>(T item) where T : class, IHaveIdentity
        {
            var connection = transaction.Connection;

            var docName = typeof(T).Name;
            var sql = $"delete from Doc.{docName} where Id = '{item.Id}'";

            var command = CreateCommand<T>(connection, sql);

            command.ExecuteNonQuery();
        }

        public class CamelCaseContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                //Change the incoming property name into Title case
                var name = string.Concat(propertyName[0].ToString().ToLower(), propertyName.Substring(1));
                return base.ResolvePropertyName(name);
            }
        }

        public T Get<T>(Guid id) where T : class, IHaveIdentity
        {
            var connection = transaction.Connection;

            var docName = typeof(T).Name;
            var sql = $"select Data from Doc.{docName} where Id = '{id}'";
            
            var command = CreateCommand<T>(connection, sql);

            var item = command.ExecuteScalar();

            var serializerSetting = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCaseContractResolver()
            };

            return item is DBNull ? default : JsonConvert.DeserializeObject<T>((string)item, serializerSetting);
        }

        public IEnumerable<T> GetAll<T>() where T : class, IHaveIdentity
        {
            var connection = transaction.Connection;

            var docName = typeof(T).Name;
            var sql = $"select Data from Doc.{docName}";

            var serializerSetting = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCaseContractResolver()
            };

            var command = CreateCommand<T>(connection, sql);

            var reader = command.ExecuteReader();

            var hasRows = reader.Read();

            if (hasRows)
            {
                do
                {
                    var item = reader.GetString(0);
                    yield return JsonConvert.DeserializeObject<T>(item, serializerSetting);
                } while (reader.Read());
            }
            else
            {
                yield return default;
            }
        }

        public IEnumerable<T> Query<T>(Func<T, bool> p) where T : class, IHaveIdentity
        {
            // layta mate
            return null;
        }

        private string CheckDocTypeExists<T>()
        {
            var docName = typeof(T).Name;
            if (docTypes.Contains(docName))
                return docName;

            docTypes.Add(docName);

            var connection = transaction.Connection;
            var createTable = string.Format(@"
IF NOT EXISTS (SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'Doc' )
	EXEC sp_executesql N'CREATE SCHEMA Doc'

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Doc.{0}') AND type in ('U'))
CREATE TABLE Doc.{0}(
	Id  uniqueidentifier NOT NULL,
	Data nvarchar(max) NOT NULL,
	PRIMARY KEY CLUSTERED 
(
	Id ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
", docName);

            var command = CreateCommand<T>(connection, createTable);
            command.ExecuteNonQuery();
            return docName;
        }

        private IDbCommand CreateCommand<T>(IDbConnection connection, string sql)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            return command;
        }
    }
}