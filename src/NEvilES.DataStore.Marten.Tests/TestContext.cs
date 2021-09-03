using System;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.Marten.Tests
{
    public class TestContext
    {
        public IServiceProvider Services { get; }

        public TestContext()
        {
            const string connString = "Host=localhost;Username=postgres;Password=password;Database=dataroom2";

            var services = new ServiceCollection()
                .AddSingleton<IDocumentStore>(c => DocumentStore.For(connString) )
                .AddScoped(s => s.GetRequiredService<IDocumentStore>().OpenSession())
                .AddScoped(s => s.GetRequiredService<IDocumentStore>().QuerySession());

            services.AddAllGenericTypes(typeof(IWriteReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });
            services.AddAllGenericTypes(typeof(IReadFromReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });

            Services = services.BuildServiceProvider();
        }
    }
}