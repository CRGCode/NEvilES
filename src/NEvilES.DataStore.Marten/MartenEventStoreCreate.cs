using NEvilES.Abstractions;
using NEvilES.Abstractions.DataStore;
using Npgsql;

namespace NEvilES.DataStore.Marten
{
    public class MartenEventStoreCreate : ICreateOrWipeDb
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
            
            using (var connection = new NpgsqlConnection($@"Host={connString.Keys["Host"]};Username=postgres;Password=password;Database=postgres"))
            {
                connection.Open();

                RunSql(connection, $@"
SELECT	pg_terminate_backend (pid)
FROM	pg_stat_activity
WHERE	datname = '{dbName}';
");
                RunSql(connection, $@"DROP DATABASE IF EXISTS {dbName};");
                RunSql(connection, $@"CREATE DATABASE {dbName};");
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
       metadata text NOT NULL,
       bodytype varchar(500) NOT NULL,
       body text NOT NULL,
       who uuid NOT NULL,
       _when timestamp NOT NULL,
       version int NOT NULL,
       appversion varchar(20) NOT NULL
)";
                cmd.ExecuteNonQuery();
            }
        }
    }
}