using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Microsoft.Extensions.Configuration;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore;
using NEvilES.DataStore.SQL;
using NEvilES.Pipeline;
using NEvilES.Tests.Sample;
using NEvilES.Tests.Sample.ReadModel;
using StructureMap;

namespace NEvilES.Tests
{
    public class SharedFixtureContext : IDisposable
    {
        private static bool runOnce = true;

        public SharedFixtureContext()
        {
            var lookup = new EventTypeLookupStrategy();
            lookup.ScanAssemblyOfType(typeof(Person.Created));
            lookup.ScanAssemblyOfType(typeof(Approval));

            var appVeyor = Environment.GetEnvironmentVariable("APPVEYOR");
            var connString = appVeyor == null ? "ES_TEST" : "AppVeyor";

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var configuration = builder.Build();

            Container = new Container(x =>
            {
                x.Scan(s =>
                {
                    s.AssemblyContainingType<Person.Created>();
                    s.AssemblyContainingType<ICommandProcessor>();

                    s.ConnectImplementationsToTypesClosing(typeof(IProcessCommand<>));
                    s.ConnectImplementationsToTypesClosing(typeof(IHandleStatelessEvent<>));
                    s.ConnectImplementationsToTypesClosing(typeof(IHandleAggregateCommandMarker<>));
                    s.ConnectImplementationsToTypesClosing(typeof(INeedExternalValidation<>));
                    s.ConnectImplementationsToTypesClosing(typeof(IProject<>));
                    s.ConnectImplementationsToTypesClosing(typeof(IProjectWithResult<>));

                    s.WithDefaultConventions();
                    s.SingleImplementationsOfInterface();
                });

                x.For<IApprovalWorkflowEngine>().Use<ApprovalWorkflowEngine>();
                x.For<ICommandProcessor>().Use<PipelineProcessor>();
                x.For<ISecurityContext>().Use<SecurityContext>();
                x.For<ICommandProcessor>().Use<PipelineProcessor>();
                x.For<IEventTypeLookupStrategy>().Add(lookup).Singleton();
                x.For<IRepository>().Use<InMemoryEventStore>();
                // x.For<IRepository>().Use<DatabaseEventStore>();
                x.For<IReadModel>().Use<TestReadModel>();

                x.For<IConnectionString>().Use(s => new ConnectionString(configuration.GetConnectionString(connString))).Singleton();
                x.For<ICommandContext>().Use("CommandContext", s => new CommandContext(new CommandContext.User(Guid.NewGuid(), 666), new Transaction(Guid.NewGuid()), new CommandContext.User(Guid.NewGuid(), 007), ""));
                x.For<IDbConnection>().Use("Connection", s => new SqlConnection(s.GetInstance<IConnectionString>().Data));
                x.For<IDbTransaction>().Use("Transaction", s => s.GetInstance<IDbConnection>().BeginTransaction());
            });

            if (runOnce)
            {
                runOnce = false;
                TestLocalDbExists(Container.GetInstance<IConnectionString>());
            }
        }

        public Container Container { get; private set; }

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
       [metadata] [nvarchar](max) NOT NULL,
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

        public void Dispose()
        {
            Container?.Dispose();
        }
    }

    public class Factory : IFactory
    {
        private readonly IContainer container;

        public Factory(IContainer container)
        {
            this.container = container;
        }

        public object Get(Type type)
        {
            return container.GetInstance(type);
        }

        public object TryGet(Type type)
        {
            return container.TryGetInstance(type);
        }

        public IEnumerable GetAll(Type type)
        {
            return container.GetAllInstances(type);
        }
    }
}