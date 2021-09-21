using System;
using GTD.Domain;
using Microsoft.Extensions.DependencyInjection;
using NEvilES;
using NEvilES.Abstractions;
using NEvilES.Abstractions.DataStore;
using NEvilES.Abstractions.Pipeline;

namespace GTD.SeedData
{
    public static class SeedData{

        public static void Initialise(string connString, IServiceProvider container)
        {
            Console.WriteLine("GTD seed data.......");

            container.GetRequiredService<ICreateOrWipeDb>().CreateOrWipeDb(new ConnectionString(connString));

            var id = CombGuid.NewGuid();

            var serviceScopeFactory = container.GetRequiredService<IServiceScopeFactory>();
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var processor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
                var craig = new User.NewUser { UserId = CombGuid.NewGuid(), Details = new User.Details("craig@test.com", "xxx", "worker", "Craig Gardiner") };
                processor.Process(craig);
                var elijah = new User.NewUser { UserId = CombGuid.NewGuid(), Details = new User.Details("elijah@test.com", "xxx", "worker", "Elijah Bates") };
                processor.Process(elijah);
                var brad = new User.NewUser { UserId = CombGuid.NewGuid(), Details = new User.Details("brad@testingABC.com", "xxx", "client", "Brad Jones") };
                processor.Process(brad);

                processor.Process(new Client.NewClient { ClientId = id, Name = "Testing ABC" });
                processor.Process(new Client.NewClient { ClientId = CombGuid.NewGuid(), Name = "FBI" });

                var project = new Project.NewProject
                {
                    ProjectId = CombGuid.NewGuid(),
                    Name = "GTD",
                    ClientId = id,
                    DefaultContacts = new[] { new Project.UserNotificationEndpoint(brad.UserId, Project.NotificationType.Email, brad.Details.Email) }
                };
                processor.Process(project);
                processor.Process(new Project.InvolveUserInProject()
                {
                    ProjectId = project.ProjectId,
                    NotificationEndpoint = new Project.UserNotificationEndpoint(craig.UserId, Project.NotificationType.Email, craig.Details.Email)
                });
                processor.Process(new Project.InvolveUserInProject()
                {
                    ProjectId = project.ProjectId,
                    NotificationEndpoint = new Project.UserNotificationEndpoint(elijah.UserId, Project.NotificationType.Email, elijah.Details.Email)
                });

                var request = new Request.NewRequest { RequestId = CombGuid.NewGuid(), ProjectId = project.ProjectId, ShortName = "Host on .Net Core", Description = "", Priority = 1 };
                processor.Process(request);

                processor.Process(new Request.CommentAdded { RequestId = request.RequestId, Text = "System test comment" });
            }
            var reader = container.GetRequiredService<IReadFromReadModel<Guid>>();
            var client = reader.Get<ReadModel.Client>(id);
            Console.WriteLine("Id {0} - {1}", id, client.Name);
        }
    }
}