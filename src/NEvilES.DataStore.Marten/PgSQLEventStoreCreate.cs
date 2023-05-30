using NEvilES.Abstractions;
using NEvilES.Abstractions.DataStore;
using Npgsql;

namespace NEvilES.DataStore.Marten
{
    public class PgSQLEventStoreCreate : ICreateOrWipeDb
    {
        private readonly string targetDb;
        private readonly string dbName;
        private readonly string master;

        public PgSQLEventStoreCreate(IConnectionString connString)
        {
            dbName = connString.Keys["Database"];

            targetDb = connString.Data;

            master = $"Host={connString.Keys["Host"]};Port={connString.Keys["Port"]};Username={connString.Keys["Username"]};Password={connString.Keys["Password"]};Database=postgres";
        }

        public void RunSql(string sql)
        {
            using var connection = new NpgsqlConnection(targetDb);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private void RunSql(NpgsqlConnection connection, string sql)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public void CreateOrWipeDb()
        {
            using (var connection = new NpgsqlConnection(master))
            {
                connection.Open();

                RunSql(connection, $@"
SELECT	pg_terminate_backend (pid)
FROM	pg_stat_activity
WHERE	datname = '{dbName}';
");
                RunSql(connection, $@"DROP DATABASE IF EXISTS {dbName};");
                NpgsqlConnection.ClearAllPools();
                RunSql(connection, $@"CREATE DATABASE {dbName};");
                NpgsqlConnection.ClearAllPools();
            }

            using (var connection = new NpgsqlConnection(targetDb))
            {
                connection.Open();
                var cmd = connection.CreateCommand();

                cmd.CommandText = @"
CREATE TABLE public.events(
    id SERIAL PRIMARY KEY,
    category varchar(500) NOT NULL,
    streamid uuid NOT NULL,
    transactionid uuid NOT NULL,
    bodytype varchar(500) NOT NULL,
    body text NOT NULL,
    who uuid NOT NULL,
    _when timestamp NOT NULL,
    version int NOT NULL,
    appversion varchar(20) NOT NULL,
    UNIQUE (streamid, version)
)";
                cmd.ExecuteNonQuery();
            }
        }


        public void CreateEventTable(IConnectionString connString)
        {
            using (var connection = new NpgsqlConnection(connString.Data))
            {
                connection.Open();
                var cmd = connection.CreateCommand();

                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS public.events(
    id SERIAL PRIMARY KEY,
    category varchar(500) NOT NULL,
    streamid uuid NOT NULL,
    transactionid uuid NOT NULL,
    bodytype varchar(500) NOT NULL,
    body text NOT NULL,
    who uuid NOT NULL,
    _when timestamp NOT NULL,
    version int NOT NULL,
    appversion varchar(20) NOT NULL,
    UNIQUE (streamid, version)
)";
                cmd.ExecuteNonQuery();
            }
        }
    }
}