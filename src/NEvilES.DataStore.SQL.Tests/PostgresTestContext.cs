using System.Data;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.Marten;
using Npgsql;
using Outbox.Abstractions;
using Weasel.Postgresql.Tables;

namespace NEvilES.DataStore.SQL.Tests
{
    public class PostgresTestContext : BaseTestContext
    {
        public PostgresTestContext() : base("Host=localhost;Port=5454;Username=postgres;Password=postgres;Database=neviles")
        {
        }

        protected override void AddServices(IServiceCollection services)
        {
            services.AddScoped(s =>
            {
                var conn = new NpgsqlConnection(s.GetRequiredService<IConnectionString>().Data);
                conn.Open();
                return conn;
            }).AddScoped<IDbConnection>(s => s.GetRequiredService<NpgsqlConnection>());

            services.AddSingleton<OutboxWorkerWorkerThread>();

            services.AddSingleton<IServiceBus, LocalServiceBus>();

            services.AddScoped(c =>
            {
                var conn = c.GetRequiredService<NpgsqlConnection>();
                return conn.BeginTransaction();
            }).AddScoped<IDbTransaction>(s => s.GetRequiredService<NpgsqlTransaction>()); 

            services.AddScoped<IOutboxRepository>(s => new SQLOutboxRepository(s.GetRequiredService<SQLEventStore>()));

            services
                .AddSingleton<IDocumentStore>(c => DocumentStore.For(ConnString))
                .AddScoped(s =>
                {
                    return s.GetRequiredService<IDocumentStore>().OpenSession();
                })
                .AddScoped(s => s.GetRequiredService<IDocumentStore>().QuerySession());

            services.AddAllGenericTypes(typeof(IWriteReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });
            services.AddAllGenericTypes(typeof(IReadFromReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });
            services.AddAllGenericTypes(typeof(IQueryFromReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });

            var pgSQL = new PgSQLEventStoreCreate(new ConnectionString(ConnString));
            pgSQL.CreateOrWipeDb();
            pgSQL.RunSql(@"
CREATE TABLE IF NOT EXISTS public.outbox(
    id SERIAL PRIMARY KEY,
    messageid uuid NOT NULL,
    messagetype varchar(200) NOT NULL,
    payload text NOT NULL,
    destination varchar(50) NOT NULL,
    createdat timestamp without time zone default (now() at time zone 'utc')
)");
        }
    }
}
