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
                    loggingBuilder.AddXUnit(this);
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                })
                .AddSingleton<IConnectionString>(c => new ConnectionString(ConnString))
                .AddScoped(c =>
                {
                    var conn = c.GetRequiredService<IDbConnection>();
                    return conn.BeginTransaction();
                })
                .AddEventStore<SQLEventStore, PipelineTransaction>(opts =>
                {
                    opts.PipelineStages = new[]
                    {
                        typeof(ValidationPipelineProcessor),
                        typeof(CommandPipelineProcessor),
                        typeof(ReadModelPipelineProcess)
                    };

                    opts.DomainAssemblyTypes = new[]
                    {
                        typeof(NEvilES.Tests.CommonDomain.Sample.Address),
                        typeof(PatchEvent)
                    };

                    opts.GetUserContext = s => new CommandContext.User(CombGuid.NewGuid());

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

            // ReSharper disable once VirtualMemberCallInConstructor
            AddServices(services);

            Container = services.BuildServiceProvider();
        }

    }
}