using System.Data;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.Marten;
using Newtonsoft.Json;
using Npgsql;
using Outbox.Abstractions;

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

            services.AddScoped(c =>
            {
                var conn = c.GetRequiredService<NpgsqlConnection>();
                return conn.BeginTransaction();
            }).AddScoped<IDbTransaction>(s => s.GetRequiredService<NpgsqlTransaction>());

            services.AddScoped<ITransaction, PipelineTransaction>();
            services.AddScoped<PipelineTransaction>();
            services.AddScoped<ISerialize, Serializer>();

            services.AddScoped<IOutboxRepository>(s => new SQLOutboxRepository(s.GetRequiredService<IDbTransaction>()));

            services.AddSingleton<OutboxWorkerSendingMessages>();
            services.AddSingleton<ITriggerOutbox>(s => s.GetRequiredService<OutboxWorkerSendingMessages>());
            services.AddScoped<IServiceBus, LocalServiceBus>();

            services
                .AddSingleton<IDocumentStore>(c => DocumentStore.For(ConnString))
                .AddScoped(s => s.GetRequiredService<IDocumentStore>().IdentitySession())
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

    public class Serializer : ISerialize
    {
        public string ToJson<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json)!;
        }
    }
}
