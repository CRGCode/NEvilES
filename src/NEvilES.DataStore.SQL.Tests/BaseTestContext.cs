using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;

namespace NEvilES.DataStore.SQL.Tests
{
    public abstract class BaseTestContext
    {
        protected readonly string ConnString;
        public IServiceProvider Container { get; }

        protected abstract void AddServices(IServiceCollection services);

        protected BaseTestContext(string connString)
        {
            ConnString = connString;
            var services = new ServiceCollection()
                .AddSingleton<IConnectionString>(c => new ConnectionString(ConnString))
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

            services.AddSingleton<IUser>(c => new CommandContext.User(Guid.Parse("00000001-0007-4852-9D2D-111111111111")));
            services.AddScoped<ICommandContext, CommandContext>(s =>
            {
                var user = s.GetRequiredService<IUser>();
                var transaction = s.GetRequiredService<ITransaction>();
                return new CommandContext(user, transaction, null, "1.0");
            });

            services.AddScoped<IReadEventStore,SQLEventStoreReader>();
            services.AddScoped<SQLDocumentRepository>();
            services.AddScoped<IReadFromReadModel>(s => s.GetRequiredService<SQLDocumentRepository>());
            services.AddScoped<IWriteReadModel>(s => s.GetRequiredService<SQLDocumentRepository>());

            AddServices(services);

            Container = services.BuildServiceProvider();


        }
    }
}