using System;
using System.Data;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;
using Xunit.Abstractions;

namespace NEvilES.DataStore.SQL.Tests
{
    public abstract class BaseTestContext :  ITestOutputHelperAccessor
    {
        protected readonly string ConnString;
        public IServiceProvider Container { get; }

        protected abstract void AddServices(IServiceCollection services);
        public ITestOutputHelper OutputHelper { get; set; }

        protected BaseTestContext(string connString)
        {
            ConnString = connString;
            var services = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.SetMinimumLevel(LogLevel.Information);
                })
                .AddSingleton<IConnectionString>(c => new ConnectionString(ConnString))
                .AddScoped(c =>
                {
                    var conn = c.GetRequiredService<IDbConnection>();
                    return conn.BeginTransaction();
                })
                .AddEventStore<SQLEventStore, PipelineTransaction>(opts =>
                {
                    opts.DomainAssemblyTypes = new[]
                    {
                        typeof(NEvilES.Tests.CommonDomain.Sample.Address),
                    };

                    opts.GetUserContext = s => new CommandContext.User(CombGuid.NewGuid());

                    opts.ReadModelAssemblyTypes = new[]
                    {
                        typeof(NEvilES.Tests.CommonDomain.Sample.Address),
                    };
                });

            services.AddLogging(configure =>
            {
                configure.AddXUnit(this);
                configure.SetMinimumLevel(LogLevel.Trace);
            });
            services.AddSingleton<IUser>(c => new CommandContext.User(Guid.Parse("00000001-0007-4852-9D2D-111111111111")));
            services.AddScoped<ICommandContext, CommandContext>(s =>
            {
                var user = s.GetRequiredService<IUser>();
                var transaction = s.GetRequiredService<ITransaction>();
                return new CommandContext(user, transaction, null, "1.0");
            });

            services.AddScoped<IReadEventStore,SQLEventStoreReader>();
            //services.AddScoped<SQLDocumentRepository>();
            //services.AddScoped<IReadFromReadModel>(s => s.GetRequiredService<SQLDocumentRepository>());
            //services.AddScoped<IWriteReadModel>(s => s.GetRequiredService<SQLDocumentRepository>());

            // ReSharper disable once VirtualMemberCallInConstructor
            AddServices(services);

            Container = services.BuildServiceProvider();
        }

    }
}