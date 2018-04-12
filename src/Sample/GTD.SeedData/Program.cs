using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Autofac;
using GTD.Common;
using GTD.Domain;
using GTD.ReadModel;
using NEvilES;
using NEvilES.DataStore;
using NEvilES.Pipeline;
using Client = GTD.Domain.Client;
using Request = GTD.Domain.Request;

namespace GTD.SeedData
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(new CommandContext.User(Guid.NewGuid())).Named<CommandContext.IUser>("user");
            builder.RegisterInstance(new InMemoryReadModel()).AsImplementedInterfaces();
            builder.RegisterInstance<IEventTypeLookupStrategy>(new EventTypeLookupStrategy());

            const string connString = "Server=(localdb)\\SQL2016;Database=ES_GTD;Trusted_Connection=True";
            builder.RegisterModule(new EventStoreDatabaseModule(connString));
            builder.RegisterModule(new EventProcessorModule(typeof(User).GetTypeInfo().Assembly, typeof(ReadModel.Client).GetTypeInfo().Assembly));

            var container = builder.Build();
            container.Resolve<IEventTypeLookupStrategy>().ScanAssemblyOfType(typeof(Domain.Client));

            //SeedData(connString, container);

            using (var scope = container.BeginLifetimeScope())
            {
                ReplayEvents.Replay(container.Resolve<IFactory>(), scope.Resolve<IAggregateHistory>());
            }
            var reader = (InMemoryReadModel)container.Resolve<IReadFromReadModel>();
            var client1 = reader.Query<ReadModel.Client>(x => x.Name == "FBI").ToArray();
            Console.WriteLine("Read Model Document Count {0}", reader.Count());
            Console.WriteLine("Done - Hit any key!");
            Console.ReadKey();
        }

        private static void SeedData(string connString, IContainer container)
        {
            Console.WriteLine("GTD seed data.......");

            EventStoreDatabaseModule.TestLocalDbExists(new ConnectionString(connString));

            var id = CombGuid.NewGuid();

            using (var scope = container.BeginLifetimeScope())
            {
                scope.Resolve<PipelineTransaction>();
                var processor = scope.Resolve<ICommandProcessor>();
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
            var reader = (InMemoryReadModel)container.Resolve<IReadFromReadModel>();
            var client = reader.Get<ReadModel.Client>(id);
            Console.WriteLine("Id {0} - {1}", id, client.Name);
        }
    }
}