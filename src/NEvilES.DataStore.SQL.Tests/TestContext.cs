using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.SQL.Tests
{
    public class TestContext
    {
        public IServiceProvider Services { get; }

        public TestContext()
        {
            const string connString = "Server=AF-004;Database=ES_GTD;Trusted_Connection=True";

            var services = new ServiceCollection()
                .AddSingleton<IConnectionString>(c => new ConnectionString(connString))
                .AddScoped<IDbConnection>(c =>
                {
                    var conn = new SqlConnection(c.GetRequiredService<IConnectionString>().Data);
                    conn.Open();
                    return conn;
                })
                .AddScoped(c =>
                {
                    var conn = c.GetService<IDbConnection>();
                    return conn.BeginTransaction();
                })
                .AddEventStore<SQLEventStore, PipelineTransaction>(opts =>
                {
                    opts.DomainAssemblyTypes = new[]
                    {
                        typeof(NEvilES.Tests.CommonDomain.Sample.Address),
                    };

                    opts.GetUserContext = s => new Pipeline.CommandContext.User(CombGuid.NewGuid());

                    opts.ReadModelAssemblyTypes = new[]
                    {
                        typeof(NEvilES.Tests.CommonDomain.Sample.Address),
                    };
                });

            services.AddSingleton<IReadEventStore,SQLEventStoreReader>();
            services.AddSingleton<SQLDocumentRepository>();
            services.AddSingleton<IReadFromReadModel>(s => s.GetRequiredService<SQLDocumentRepository>());
            services.AddSingleton<IWriteReadModel>(s => s.GetRequiredService<SQLDocumentRepository>());

            Services = services.BuildServiceProvider();
        }
    }
}