using NEvilES.Abstractions;
using NEvilES.Abstractions.DataStore;
using Npgsql;

namespace NEvilES.DataStore.Marten
{
    public class PgSQLEventStoreCreate : ICreateOrWipeDb
    {
        public void CreateOrWipeDb(IConnectionString connString)
        {
            void RunSql(NpgsqlConnection connection, string sql)
            {
                var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
            var dbName = connString.Keys["Database"];
            
            using (var connection = new NpgsqlConnection($@"Host={connString.Keys["Host"]};Port={connString.Keys["Port"]};Username=postgres;Password={connString.Keys["Password"]};Database=postgres"))
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

            using (var connection = new NpgsqlConnection(connString.Data))
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