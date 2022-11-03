using System.Data;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.Marten;
using Npgsql;

namespace NEvilES.DataStore.SQL.Tests
{
    public class PostgresTestContext : BaseTestContext
    {
        public PostgresTestContext() : base("Host=localhost;Port=5454;Username=postgres;Password=postgres;Database=neviles")
        {
        }

        protected override void AddServices(IServiceCollection services)
        {
            services.AddScoped<IDbConnection>(c =>
            {
                var conn = new NpgsqlConnection(c.GetRequiredService<IConnectionString>().Data);
                conn.Open();
                return conn;
            });

            services
                .AddSingleton<IDocumentStore>(c => DocumentStore.For(ConnString) )
                .AddScoped(s => s.GetRequiredService<IDocumentStore>().OpenSession())
                .AddScoped(s => s.GetRequiredService<IDocumentStore>().QuerySession());

            services.AddAllGenericTypes(typeof(IWriteReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });
            services.AddAllGenericTypes(typeof(IReadFromReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });
            services.AddAllGenericTypes(typeof(IQueryFromReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });

            new PgSQLEventStoreCreate().CreateOrWipeDb(new ConnectionString(ConnString));
        }
    }
}
