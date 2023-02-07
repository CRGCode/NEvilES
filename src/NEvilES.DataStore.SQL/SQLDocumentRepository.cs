using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using NEvilES.Abstractions.Pipeline;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NEvilES.DataStore.SQL
{

    // NOTE 
    // TODO - This is not general and needs work as it has MS SQL specific T-SQL

    public abstract class SQLDocumentRepository<TId> : IReadFromReadModel<TId>, IWriteReadModel<TId>, IQueryFromReadModel<TId>
    {
        private readonly IDbTransaction transaction;
        private readonly HashSet<string> docTypes;

        public SQLDocumentRepository(IDbTransaction transaction)
        {
            this.transaction = transaction;
            docTypes = new HashSet<string>();
        }

        public void Insert<T>(T item) where T : class, IHaveIdentity<TId>
        {
            var docName = CheckDocTypeExists<T>();

            var connection = transaction.Connection;

            object json = JsonConvert.SerializeObject(item);
            var sql = $"insert into Doc.{docName} values ('{item.Id}','{json}')";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
        }

        public void Update<T>(T item) where T : class, IHaveIdentity<TId>
        {
            var docName = CheckDocTypeExists<T>();

            var connection = transaction.Connection;

            var json = JsonConvert.SerializeObject(item);
            var sql = $"update Doc.{docName} set Data = '{json}' where Id = '{item.Id}'";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
        }

        public void Save<T>(T item) where T : class, IHaveIdentity<TId>
        {
            var docName = CheckDocTypeExists<T>();

            var connection = transaction.Connection;

            var json = JsonConvert.SerializeObject(item);
            var sql = @$"
IF EXISTS (SELECT 1 FROM Doc.{docName} WHERE Id = '{item.Id}')
	update Doc.{docName} set Data = 'xxx' where Id = '{item.Id}'
ELSE
	insert into Doc.{docName} values ('{item.Id}','{json}')";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
        }

        public void Delete<T>(T item) where T : class, IHaveIdentity<TId>
        {
            var connection = transaction.Connection;

            var docName = typeof(T).Name;
            var sql = $"delete from Doc.{docName} where Id = '{item.Id}'";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
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

        public T Get<T>(TId id) where T : class, IHaveIdentity<TId>
        {
            var connection = transaction.Connection;

            var docName = typeof(T).Name;
            var sql = $"select Data from Doc.{docName} where Id = '{id}'";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                var item = command.ExecuteScalar();

                var serializerSetting = new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCaseContractResolver()
                };

                return item is DBNull ? default : JsonConvert.DeserializeObject<T>((string)item, serializerSetting);
            }
        }

        public IQueryable<T> GetAll<T>() where T : class, IHaveIdentity<TId>
        {
            return new EnumerableQuery<T>(All<T>());
        }

        private IEnumerable<T> All<T>() where T : class, IHaveIdentity<TId>
        {
            var connection = transaction.Connection;

            var docName = typeof(T).Name;
            var sql = $"select Data from Doc.{docName}";

            var serializerSetting = new JsonSerializerSettings
            {
                ContractResolver = new CamelCaseContractResolver()
            };

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;

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
            reader.Close();
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, bool>> p) where T : class, IHaveIdentity<TId>
        {
            // layta mate
            throw new NotImplementedException();
        }

        public IQueryable<T> GetQuery<T>(Expression<Func<T, bool>> p) where T : class, IHaveIdentity<TId>
        {
            // layta mate
            throw new NotImplementedException();
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

            using var command = connection.CreateCommand();
            command.CommandText = createTable;
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
            return docName;
        }

        public void WipeDocTypeIfExists<T>()
        {
            var docName = typeof(T).Name;

            var connection = transaction.Connection;
            var dropTable = string.Format(@"
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Doc.{0}') AND type in ('U'))
DROP TABLE Doc.{0}", docName);

            using var command = connection.CreateCommand();
            command.CommandText = dropTable;
            command.Transaction = transaction;
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }
    }

    public class DocumentRepositoryWithKeyTypeGuid : SQLDocumentRepository<Guid>
    {
        public DocumentRepositoryWithKeyTypeGuid(IDbTransaction transaction) : base(transaction)
        {
        }
    }

    public class DocumentRepositoryWithKeyTypeString : SQLDocumentRepository<string>
    {
        public DocumentRepositoryWithKeyTypeString(IDbTransaction transaction) : base(transaction)
        {
        }
    }

}