using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GTD.Domain;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using Client = GTD.Domain.Client;
using Request = GTD.Domain.Request;

namespace GTD.SeedData
{
    public class LoadTest
    {
        private readonly ServiceProvider container;
        private readonly Dictionary<Guid, Guid[]> clients;
        private readonly Guid[] projects;
        private readonly Guid[] requests;

        public LoadTest(ServiceProvider container, int clients, int projects, int requests)
        {
            this.container = container;
            this.clients = CreateClients(clients);
            this.projects = CreateProjects(projects);
            this.requests = CreateRequests(requests);
        }

        public void Begin(int workers, int notes)
        {
            Console.WriteLine("Begin workers");
            var tasks = new List<Task>();

            for (int i = 0; i < workers; i++)
            {
                var t = new Task(() => AddItems(notes));
                tasks.Add(t);
                t.Start();
            }

            Task.WaitAll(tasks.ToArray(), CancellationToken.None);
        }

        void RunCommand<TCommand>(TCommand command) where TCommand : ICommand
        {
            var processor = container.GetRequiredService<ICommandProcessor>();
            processor.Process(command);
        }

        private void AddItems(int items)
        {
            foreach (var request in requests)
            {
                for (int i = 0; i < items; i++)
                {
                    RunCommand(new Request.AddComment
                    {
                        StreamId = request,
                        Text = "Note",
                    });
                }
            }
        }

        private Dictionary<Guid, Guid[]> CreateClients(int count)
        {
            var items = new Dictionary<Guid, Guid[]>();
            for (int i = 0; i < count; i++)
            {
                var id = Guid.NewGuid();
                RunCommand(new Client.NewClient()
                {
                    StreamId = id,
                    Name = $"Client {count} - {id}",
                });
                var users = CreateUsers(id, 4);
                items.Add(id, users);
            }

            return items;
        }

        private Guid[] CreateUsers(Guid clientId, int count)
        {
            var items = new List<Guid>();
            var serviceScopeFactory = container.GetRequiredService<IServiceScopeFactory>();
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var processor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
                for (int i = 0; i < count; i++)
                {
                    var id = Guid.NewGuid();
                    processor.Process(new User.NewUser()
                    {
                        StreamId = id,
                        ClientGroup = clientId,
                        Details = new User.Details($"user_{id}@test.com", "pwd", "roles....", $"User{count}")
                    });
                    items.Add(id);
                }
            }

            return items.ToArray();
        }

        private Guid[] CreateProjects(int count)
        {
            var items = new List<Guid>();
            var serviceScopeFactory = container.GetRequiredService<IServiceScopeFactory>();
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var processor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
                foreach (var client in clients)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var id = Guid.NewGuid();
                        processor.Process(new Project.NewProject()
                        {
                            StreamId = id,
                            Name = $"Project {count} - {id}",
                            ClientId = client.Key,
                            DefaultContacts = new[]
                            {
                                new Project.UserNotificationEndpoint(client.Value.First(),Project.NotificationType.Mobile,"endpoint")
                            },
                        });
                        items.Add(id);
                    }
                }
            }

            return items.ToArray();
        }


        private Guid[] CreateRequests(int count)
        {
            var items = new List<Guid>();
            var serviceScopeFactory = container.GetRequiredService<IServiceScopeFactory>();
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var processor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
                foreach (var projectId in projects)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var id = Guid.NewGuid();
                        processor.Process(new Request.NewRequest()
                        {
                            StreamId = id,
                            ProjectId = projectId,
                            Description = $"Request {id}",
                            Priority = 5,
                        });
                        items.Add(id);
                    }
                }
            }

            return items.ToArray();
        }
    }
}