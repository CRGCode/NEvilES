using System;
using System.Data;
using System.Data.SqlClient;
using GTD.Domain;
using Microsoft.Extensions.DependencyInjection;
using NEvilES;
using NEvilES.Abstractions;
using NEvilES.Abstractions.DataStore;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.MSSQL;
using NEvilES.DataStore.SQL;
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

            services.AddSingleton<IUser>(c => CommandContext.User.NullUser());
            services.AddSingleton<ICreateOrWipeDb, MSSQLEventStoreCreate>();

            services.AddScoped<ICommandContext, CommandContext>(s =>
                new CommandContext(s.GetRequiredService<IUser>(), s.GetRequiredService<ITransaction>(), null, "1.0"));

            services.AddScoped<SQLDocumentRepository>();
            services.AddScoped<IReadFromReadModel>(s => s.GetRequiredService<SQLDocumentRepository>());
            services.AddScoped<IWriteReadModel>(s => s.GetRequiredService<SQLDocumentRepository>());

            var container =  services.BuildServiceProvider();

            SeedData(connString, container);

            //using (var scope = container.CreateScope())
            //{
            //    ReplayEvents.Replay(container.GetService<IFactory>(), scope.ServiceProvider.GetRequiredService<IAggregateHistory>());
            //}
            //var reader = (InMemoryDocumentRepository)container.GetService<IReadFromReadModel>();
            //var client1 = reader.Query<ReadModel.Client>(x => x.Name == "FBI").ToArray();
            //Console.WriteLine("Read Model Document Count {0}", reader.Count());
            Console.WriteLine("Done - Hit any key!");
            Console.ReadKey();
        }

        private static void SeedData(string connString, IServiceProvider container)
        {
            Console.WriteLine("GTD seed data.......");

            container.GetRequiredService<ICreateOrWipeDb>().CreateOrWipeDb(new ConnectionString(connString));

            var id = CombGuid.NewGuid();

            var serviceScopeFactory = container.GetRequiredService<IServiceScopeFactory>();
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var processor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
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
            var reader = container.GetRequiredService<IReadFromReadModel>();
            var client = reader.Get<ReadModel.Client>(id);
            Console.WriteLine("Id {0} - {1}", id, client.Name);
        }
    }
}