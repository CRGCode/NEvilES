using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using GTD.Domain;
using GTD.ReadModel;
using Microsoft.Extensions.DependencyInjection;
using NEvilES;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.SQL;
using NEvilES.Extensions.DependencyInjection;
using NEvilES.Pipeline;
using Client = GTD.Domain.Client;
using Request = GTD.Domain.Request;

namespace GTD.SeedData
{
    class Program
    {
        static void Main(string[] args)
        {
            const string connString = "Server=AF-004;Database=ES_GTD;Trusted_Connection=True";

            var services = new ServiceCollection()
                .AddSingleton<IConnectionString>(c => new ConnectionString(connString))
                .AddSingleton<IUser>(c => CommandContext.User.NullUser())
                .AddScoped<IDbConnection>(c =>
                {
                    var conn = new SqlConnection(c.GetService<IConnectionString>().Data);
                    conn.Open();
                    return conn;
                })
                .AddScoped(c =>
                {
                    var conn = c.GetService<IDbConnection>();
                    return conn.BeginTransaction();
                })
                .AddEventStore<DatabaseEventStore, PipelineTransaction>(opts =>
                {
                    opts.DomainAssemblyTypes = new[]
                    {
                        typeof(Client),
                    };

                    opts.GetUserContext = s => s.GetService<IUser>() ?? throw new Exception("No User Context");

                    opts.ReadModelAssemblyTypes = new[]
                    {
                        typeof(ReadModel.Client)
                    };
                });

            services.AddSingleton<DocumentStore>();
            services.AddSingleton<IReadFromReadModel>(s => s.GetRequiredService<DocumentStore>());
            services.AddSingleton<IWriteReadModel>(s => s.GetRequiredService<DocumentStore>());

            var container =  services.BuildServiceProvider();

            SeedData(connString, container);

            //using (var scope = container.CreateScope())
            //{
            //    ReplayEvents.Replay(container.GetService<IFactory>(), scope.ServiceProvider.GetRequiredService<IAggregateHistory>());
            //}
            //var reader = (InMemoryReadModel)container.GetService<IReadFromReadModel>();
            //var client1 = reader.Query<ReadModel.Client>(x => x.Name == "FBI").ToArray();
            //Console.WriteLine("Read Model Document Count {0}", reader.Count());
            Console.WriteLine("Done - Hit any key!");
            Console.ReadKey();
        }

        private static void SeedData(string connString, IServiceProvider container)
        {
            Console.WriteLine("GTD seed data.......");

            TestLocalDbExists(new ConnectionString(connString));

            var id = CombGuid.NewGuid();

            using (var scope = container.CreateScope())
            {
                scope.ServiceProvider.GetService<PipelineTransaction>();  // What's this all about?
                var processor = scope.ServiceProvider.GetService<ICommandProcessor>();
                var craig = new User.NewUser { StreamId = CombGuid.NewGuid(), Details = new User.Details("craig@test.com", "xxx", "worker", "Craig Gardiner") };
                processor.Process(craig);
                var elijah = new User.NewUser { StreamId = CombGuid.NewGuid(), Details = new User.Details("elijah@test.com", "xxx", "worker", "Elijah Bates") };
                processor.Process(elijah);
                var brad = new User.NewUser { StreamId = CombGuid.NewGuid(), Details = new User.Details("brad@testingABC.com", "xxx", "client", "Brad Jones") };
                processor.Process(brad);

                processor.Process(new Client.NewClient { StreamId = id, Name = "Testing ABC" });
                processor.Process(new Client.NewClient { StreamId = CombGuid.NewGuid(), Name = "FBI" });

                var project = new Project.NewProject
                {
                    StreamId = CombGuid.NewGuid(),
                    Name = "GTD",
                    ClientId = id,
                    DefaultContacts = new[] { new Project.UserNotificationEndpoint(brad.StreamId, Project.NotificationType.Email, brad.Details.Email) }
                };
                processor.Process(project);
                processor.Process(new Project.InvolveUserInProject()
                {
                    StreamId = project.StreamId,
                    NotificationEndpoint = new Project.UserNotificationEndpoint(craig.StreamId, Project.NotificationType.Email, craig.Details.Email)
                });
                processor.Process(new Project.InvolveUserInProject()
                {
                    StreamId = project.StreamId,
                    NotificationEndpoint = new Project.UserNotificationEndpoint(elijah.StreamId, Project.NotificationType.Email, elijah.Details.Email)
                });

                var request = new Request.NewRequest { StreamId = CombGuid.NewGuid(), ProjectId = project.StreamId, ShortName = "Host on .Net Core", Description = "", Priority = 1 };
                processor.Process(request);

                processor.Process(new Request.CommentAdded { StreamId = request.StreamId, Text = "System test comment" });
            }
            var reader = container.GetService<IReadFromReadModel>();
            var client = reader.Get<ReadModel.Client>(id);
            Console.WriteLine("Id {0} - {1}", id, client.Name);
        }

        public static void TestLocalDbExists(IConnectionString connString)
        {
            using (var connection = new SqlConnection($@"Server={connString.Keys["Server"]};Database=Master;Integrated Security=true;"))
            {
                connection.Open();

                var createDb = string.Format(@"
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
    }
}