using System;
using System.Data;
using GTD.Domain;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NEvilES;
using NEvilES.Abstractions;
using NEvilES.Abstractions.DataStore;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.Marten;
using NEvilES.DataStore.SQL;
using NEvilES.Pipeline;
using Npgsql;

namespace GTD.SeedData
{
    public static class PostgresStartup
    {
        public static ServiceProvider RegisterServices(string connString)
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IDocumentStore>(c => DocumentStore.For(connString))
                .AddScoped(s => s.GetRequiredService<IDocumentStore>().OpenSession())
                .AddScoped(s => s.GetRequiredService<IDocumentStore>().QuerySession())
                .AddSingleton<IConnectionString>(c => new ConnectionString(connString))
                .AddScoped<IDbConnection>(c =>
                {
                    var conn = new NpgsqlConnection(c.GetRequiredService<IConnectionString>().Data);
                    conn.Open();
                    return conn;
                })
                .AddScoped(c =>
                {
                    var conn = c.GetRequiredService<IDbConnection>();
                    return conn.BeginTransaction();
                })
                .AddEventStore<SQLEventStore, PipelineTransaction>(opts =>
                {
                    opts.DomainAssemblyTypes = new[]
                    {
                        typeof(Client),
                    };

                    opts.GetUserContext = s => ServiceProviderServiceExtensions.GetService<IUser>(s) ?? throw new Exception("No User Context");

                    opts.ReadModelAssemblyTypes = new[]
                    {
                        typeof(ReadModel.Client)
                    };
                });


            services.AddSingleton<IUser>(c => CommandContext.User.NullUser());
            services.AddSingleton<ICreateOrWipeDb, PgSQLEventStoreCreate>();

            services.AddScoped<ICommandContext, CommandContext>(s =>
                new CommandContext(s.GetRequiredService<IUser>(), s.GetRequiredService<ITransaction>(), null, "1.1"));

            services.AddAllGenericTypes(typeof(IWriteReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });
            services.AddAllGenericTypes(typeof(IReadFromReadModel<>), new[] { typeof(MartenDocumentRepository<>).Assembly });

            return services.BuildServiceProvider();
        }
    }
}