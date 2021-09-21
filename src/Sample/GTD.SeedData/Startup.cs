using System;
using System.Data;
using System.Data.SqlClient;
using GTD.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NEvilES;
using NEvilES.Abstractions;
using NEvilES.Abstractions.DataStore;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.MSSQL;
using NEvilES.DataStore.SQL;
using NEvilES.Pipeline;

namespace GTD.SeedData
{
    public static class Startup {

        public static ServiceProvider Start(string connString)
        {
            var services = new ServiceCollection()
                .AddSingleton<IConnectionString>(c => new ConnectionString(connString))
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddConsole();
                    loggingBuilder.SetMinimumLevel(LogLevel.Information);

                })
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
                        typeof(Client),
                    };

                    opts.GetUserContext = s => ServiceProviderServiceExtensions.GetService<IUser>(s) ?? throw new Exception("No User Context");

                    opts.ReadModelAssemblyTypes = new[]
                    {
                        typeof(ReadModel.Client)
                    };
                });

            services.AddSingleton<IUser>(c => CommandContext.User.NullUser());
            services.AddSingleton<ICreateOrWipeDb, MSSQLEventStoreCreate>();

            services.AddScoped<ICommandContext, CommandContext>(s =>
                new CommandContext(s.GetRequiredService<IUser>(), s.GetRequiredService<ITransaction>(), null, "1.0"));

            services.AddAllGenericTypes(typeof(IWriteReadModel<>), new[] { typeof(SQLDocumentRepository<>).Assembly });
            services.AddAllGenericTypes(typeof(IReadFromReadModel<>), new[] { typeof(SQLDocumentRepository<>).Assembly });

            var container = services.BuildServiceProvider();
            return container;
        }
    }
}