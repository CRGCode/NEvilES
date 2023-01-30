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
using NEvilES.Abstractions.Pipeline.Async;
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
                    opts.DomainAssemblyTypes = new[]
                    {
                        typeof(Person.Created),
                        //typeof(Employee.Aggregate),
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
            services.AddSingleton<DocumentStoreGuid>();
            services.AddSingleton<IReadFromReadModel<Guid>>(s => s.GetRequiredService<DocumentStoreGuid>());
            services.AddSingleton<IWriteReadModel<Guid>>(s => s.GetRequiredService<DocumentStoreGuid>());
            services.AddSingleton<DocumentStoreString>();
            services.AddSingleton<IReadFromReadModel<string>>(s => s.GetRequiredService<DocumentStoreString>());
            services.AddSingleton<IWriteReadModel<string>>(s => s.GetRequiredService<DocumentStoreString>());


            services.AddScoped<IDbConnection>(c =>
            {
                var conn = new SqlConnection(c.GetRequiredService<IConnectionString>().Data);
                conn.Open();
                return conn;
            });

            services.AddScoped<TaxRuleEngine>();
            services.AddScoped<IApprovalWorkflowEngine, ApprovalWorkflowEngine>();

            Container = services.BuildServiceProvider();




            //////////
            //var lookup = new EventTypeLookupStrategy();
            //lookup.ScanAssemblyOfType(typeof(Details.Created));
            //lookup.ScanAssemblyOfType(typeof(Approval));

            //Container = new Container(x =>
            //{
            //    x.Scan(s =>
            //    {
            //        s.AssemblyContainingType<Details.Created>();
            //        s.AssemblyContainingType<Approval.Create>();
            //        s.AssemblyContainingType<ICommandProcessor>();

            //        s.ConnectImplementationsToTypesClosing(typeof(IProcessCommand<>));
            //        s.ConnectImplementationsToTypesClosing(typeof(IHandleStatelessEvent<>));
            //        s.ConnectImplementationsToTypesClosing(typeof(IHandleAggregateCommandMarker<>));
            //        s.ConnectImplementationsToTypesClosing(typeof(INeedExternalValidation<>));
            //        s.ConnectImplementationsToTypesClosing(typeof(IProject<>));
            //        s.ConnectImplementationsToTypesClosing(typeof(IProjectWithResult<>));

            //        s.WithDefaultConventions();
            //        s.SingleImplementationsOfInterface();
            //    });

            //    x.For<IApprovalWorkflowEngine>().Use<ApprovalWorkflowEngine>();
            //    x.For<ICommandProcessor>().Use<PipelineProcessor>();
            //    x.For<ISecurityContext>().Use<SecurityContext>();
            //    x.For<ICommandProcessor>().Use<PipelineProcessor>();
            //    x.For<IEventTypeLookupStrategy>().Add(lookup).Singleton();
            //    x.For<IRepository>().Use<InMemoryEventStore>();
            //    // x.For<IRepository>().Use<SQLEventStore>();
            //    x.For<IReadModel>().Use<TestReadModel>();
            //    x.For<IFactory>().Use<Factory>();

            //    x.For<IConnectionString>().Use(s => new ConnectionString(configuration.GetConnectionString(connString))).Singleton();
            //    x.For<ICommandContext>().Use("CommandContext", s => new CommandContext(new CommandContext.User(Guid.NewGuid(), 666), new Transaction(Guid.NewGuid()), new CommandContext.User(Guid.NewGuid(), 007), ""));
            //    x.For<IDbConnection>().Use("Connection", s => new SqlConnection(s.GetRequiredService<IConnectionString>().Data));
            //    x.For<IDbTransaction>().Use("Transaction", s => s.GetRequiredService<IDbConnection>().BeginTransaction());
            //});

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