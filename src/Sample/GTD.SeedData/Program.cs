using System;
using Autofac;
using GTD.Common;
using GTD.Domain;
using NEvilES;
using NEvilES.DataStore;
using NEvilES.Pipeline;

namespace GTD.SeedData
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("GTD seed data.......");

            var builder = new ContainerBuilder();
            builder.RegisterInstance(new CommandContext.User(Guid.NewGuid())).Named<CommandContext.IUser>("user");

            builder.RegisterModule(
                new EventStoreDatabaseModule("Server=(localdb)\\SQL2016;Database=es_test;Integrated Security=true"));

            var container = Register<IContainer>.Build(builder);

            EventStoreDatabaseModule.TestLocalDbExists(container.Resolve<IConnectionString>());

            container.Resolve<IEventTypeLookupStrategy>().ScanAssemblyOfType(typeof(Domain.Client));

            var id = CombGuid.NewGuid();

            using (container.Resolve<PipelineTransaction>())
            {
                var processor = container.Resolve<ICommandProcessor>();

                processor.Process(new User.NewUser {StreamId = CombGuid.NewGuid(), Details = new User.Details("craig@test.com", "xxx", "worker", "Craig Gardiner")});
                processor.Process(new User.NewUser {StreamId = CombGuid.NewGuid(), Details = new User.Details("elijah@test.com","xxx","worker","Elijah Bates")});
                var brad = new User.NewUser { StreamId = CombGuid.NewGuid(), Details = new User.Details("brad@testingABC.com", "xxx", "client", "Brad Jones") };
                processor.Process(brad);

                processor.Process(new Client.NewClient {StreamId = id, Name = "Testing ABC"});
                processor.Process(new Client.NewClient {StreamId = CombGuid.NewGuid(), Name = "FBI"});

                var project = new Project.NewProject
                {
                    StreamId = CombGuid.NewGuid(),
                    Name = "GTD",
                    ClientId = id,
                    DefaultContacts = new[] { new Project.UserNotificationEndpoint(brad.StreamId, Project.NotificationType.Email, brad.Details.Email) }
                };
                processor.Process(project);

                var request = new Request.NewRequest { StreamId = CombGuid.NewGuid(), ProjectId  = project.StreamId, ShortName = "Host on .Net Core", Description = "", Priority = 1 };
                processor.Process(request);

                processor.Process(new Request.CommentAdded { StreamId = request.StreamId, Text = "System test comment" });
            }

            var reader = container.Resolve<IReadData>();
            var client = reader.Get<ReadModel.Client>(id);
            Console.WriteLine("Id {0} - {1}",id, client.Name);
            Console.WriteLine("Done - Hit any key!");
            Console.ReadKey();
        }
    }
}