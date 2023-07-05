using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.SQL;
using NEvilES.Pipeline;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit.Abstractions;

namespace NEvilES.Tests
{
    public class SharedFixtureContext :  ITestOutputHelperAccessor
    {
        public IServiceProvider Container { get; }
        public ITestOutputHelper OutputHelper { get; set; }

        private static bool runOnce = true;

        public SharedFixtureContext()
        {
            var appVeyor = Environment.GetEnvironmentVariable("APPVEYOR");
            var connString = appVeyor == null ? "ES_TEST" : "AppVeyor";

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            var services = new ServiceCollection()
                .AddSingleton<IConnectionString>(c => new ConnectionString(configuration.GetConnectionString(connString)))
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
                        typeof(Person.Created),
                        typeof(Approval),
                        typeof(UniqueNameValidation)
                    };

                    opts.GetUserContext = s => new CommandContext.User(CombGuid.NewGuid());

                    opts.ReadModelAssemblyTypes = new[]
                    {
                        typeof(Person.Created),
                    };
                });

            services.AddLogging(configure => configure.AddXUnit(this).SetMinimumLevel(LogLevel.Trace));
            
            services.AddSingleton<IUser>(c => new CommandContext.User(Guid.Parse("00000001-0007-4852-9D2D-111111111111")));
            services.AddScoped<ICommandContext, CommandContext>(s =>
            {
                var user = s.GetRequiredService<IUser>();
                var transaction = s.GetRequiredService<ITransaction>();
                return new CommandContext(user, transaction, null, "1.0");
            });

            services.AddScoped<IFactory, ServiceProviderFactory>();
            services.AddScoped<IReadEventStore, SQLEventStoreReader>();
            services.AddSingleton<IDocumentMemory, DocumentMemory>();
            services.AddScoped<DocumentStoreGuid>();
            services.AddScoped<IReadFromReadModel<Guid>>(s => s.GetRequiredService<DocumentStoreGuid>());
            services.AddScoped<IWriteReadModel<Guid>>(s => s.GetRequiredService<DocumentStoreGuid>());
            services.AddScoped<DocumentStoreString>();
            services.AddScoped<IReadFromReadModel<string>>(s => s.GetRequiredService<DocumentStoreString>());
            services.AddScoped<IWriteReadModel<string>>(s => s.GetRequiredService<DocumentStoreString>());


            services.AddScoped<IDbConnection>(c =>
            {
                var conn = new SqlConnection(c.GetRequiredService<IConnectionString>().Data);
                conn.Open();
                return conn;
            });

            services.AddScoped<TaxRuleEngine>();
            services.AddScoped<IApprovalWorkflowEngine, ApprovalWorkflowEngine>();

            Container = services.BuildServiceProvider();

            if (runOnce)
            {
                runOnce = false;
                TestLocalDbExists(Container.GetRequiredService<IConnectionString>());
            }
        }

        public static void TestLocalDbExists(IConnectionString connString)
        {
            using (var connection = new SqlConnection(string.Format(@"Server={0};Database=Master;Integrated Security=true;", connString.Keys["Server"])))
            {
                connection.Open();

                var createDb = string.Format(
                    @"
IF EXISTS(SELECT * FROM sys.databases WHERE name='{0}')
BEGIN
	ALTER DATABASE [{0}]
	SET SINGLE_USER
	WITH ROLLBACK IMMEDIATE
	DROP DATABASE [{0}]
END

DECLARE @FILENAME AS VARCHAR(255)

SET @FILENAME = CONVERT(VARCHAR(255), SERVERPROPERTY('instancedefaultdatapath')) + '{0}';

EXEC ('CREATE DATABASE [{0}] ON PRIMARY
	(NAME = [{0}],
	FILENAME =''' + @FILENAME + ''',
	SIZE = 25MB,
	MAXSIZE = 50MB,
	FILEGROWTH = 5MB )')
", connString.Keys["Database"]);

                var command = connection.CreateCommand();
                command.CommandText = createDb;
                command.ExecuteNonQuery();

                //command.CommandText = @"ALTER LOGIN [sa] WITH PASSWORD=N'Password12!'";
                //command.ExecuteNonQuery();

                //command.CommandText = "ALTER LOGIN[sa] ENABLE";
                //command.ExecuteNonQuery();
            }

            using (var connection = new SqlConnection(connString.Data))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = @"
CREATE TABLE [dbo].[events](
       [id] [bigint] IDENTITY(1,1) NOT NULL,
       [category] [nvarchar](500) NOT NULL,
       [streamid] [uniqueidentifier] NOT NULL,
       [transactionid] [uniqueidentifier] NOT NULL,
       [bodytype] [nvarchar](500) NOT NULL,
       [body] [nvarchar](max) NOT NULL,
       [who] [uniqueidentifier] NOT NULL,
       [_when] [datetime] NOT NULL,
       [version] [int] NOT NULL,
       [appversion] [nvarchar](20) NOT NULL,
PRIMARY KEY CLUSTERED
(
       [id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
";
                command.ExecuteNonQuery();
            }
        }

    }
}