using System;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.Marten.Tests
{
    public class TestContext
    {
        public static string ConnString = "Host=localhost;Port=5454;Username=postgres;Password=postgres;Database=neviles";
        public IServiceProvider Services { get; }

        public TestContext()
        {

            var services = new ServiceCollection()
                .AddSingleton<IDocumentStore>(c => DocumentStore.For(ConnString) )
                .AddScoped(s => s.GetRequiredService<IDocumentStore>().OpenSession())
                .AddScoped(s => s.GetRequiredService<IDocumentStore>().QuerySession());

            services.AddAllGenericTypes(typeof(IWriteReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });
            services.AddAllGenericTypes(typeof(IReadFromReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });
            services.AddAllGenericTypes(typeof(IQueryFromReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });

            Services = services.BuildServiceProvider();

            new PgSQLEventStoreCreate().CreateOrWipeDb(new ConnectionString(ConnString));
        }
    }
}