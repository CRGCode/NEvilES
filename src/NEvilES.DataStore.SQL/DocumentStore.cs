using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using NEvilES.Abstractions.Pipeline;
using Newtonsoft.Json;

namespace NEvilES.DataStore.SQL
{
    public class DocumentStore : IReadFromReadModel, IWriteReadModel
    {
        private readonly IConnectionString connString;
        private readonly HashSet<string> docTypes;

        public DocumentStore(IConnectionString connectionString)
        {
            connString = connectionString;
            docTypes = new HashSet<string>();
        }

        public void Insert<T>(T item) where T : class, IHaveIdentity
        {
            var docName = CheckDocTypeExists<T>();

            using var connection = OpenConnection();

            object json = JsonConvert.SerializeObject(item);
            var sql = $"insert into Doc.{docName} values ('{item.Id}','{json}')";
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public void Update<T>(T item) where T : class, IHaveIdentity
        {
            var docName = CheckDocTypeExists<T>();

            using var connection = OpenConnection();

            var json = JsonConvert.SerializeObject(item);
            var sql = $"update Doc.{docName} set Data = '{json}' where Id = '{item.Id}'";
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public void Save<T>(T item) where T : class, IHaveIdentity
        {
            var docName = CheckDocTypeExists<T>();

            using var connection = OpenConnection();

            var json = JsonConvert.SerializeObject(item);
            var sql = @$"
IF EXISTS (SELECT 1 FROM Doc.{docName} WHERE Id = '{item.Id}')
	update Doc.{docName} set Data = 'xxx' where Id = '{item.Id}'
ELSE
	insert into Doc.{docName} values ('{item.Id}','{json}')";

            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public void Delete<T>(T item) where T : class, IHaveIdentity
        {
            using var connection = OpenConnection();

            var docName = typeof(T).Name;
            var sql = $"delete from Doc.{docName} where Id = '{item.Id}'";

            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public T Get<T>(Guid id) where T : class, IHaveIdentity
        {
            using var connection = OpenConnection();

            var docName = typeof(T).Name;
            var sql = $"select Data from Doc.{docName} where Id = '{id}'";

            var command = connection.CreateCommand();
            command.CommandText = sql;
            var item = command.ExecuteScalar();

            return item is DBNull ? default : JsonConvert.DeserializeObject<T>((string)item);
        }

        public IEnumerable<T> Query<T>(Func<T, bool> p) where T : class, IHaveIdentity
        {
            // layta mate
            return null;
        }

        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection($@"Server={connString.Keys["Server"]};Database={connString.Keys["Database"]};Integrated Security=true;");
            connection.Open();
            return connection;
        }

        private string CheckDocTypeExists<T>()
        {
            var docName = typeof(T).Name;
            if (docTypes.Contains(docName))
                return docName;

            docTypes.Add(docName);

            using (var connection = OpenConnection())
            {
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
", docName);

                var command = connection.CreateCommand();
                command.CommandText = createTable;
                command.ExecuteNonQuery();
            }
            return docName;
        }
    }
}