using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using NEvilES.DataStore;
using NEvilES.Pipeline;
using NEvilES.Tests.Sample;
using NEvilES.Tests.Sample.ReadModel;
using StructureMap;

namespace NEvilES.Tests
{
    public class SharedFixtureContext
    {
        public SharedFixtureContext()
        {
            var lookup = new EventTypeLookupStrategy();
            lookup.ScanAssemblyOfType(typeof(Person.Created));
            lookup.ScanAssemblyOfType(typeof(Approval));

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
                x.For<IEventTypeLookupStrategy>().Add(lookup).Singleton();
                x.For<IRepository>().Use<InMemoryEventStore>();
                x.For<IReadModel>().Use<TestReadModel>();

                x.For<CommandContext>().Use("CommandContext", s => new CommandContext(new CommandContext.User(Guid.NewGuid(), 666), new Transaction(Guid.NewGuid()), new CommandContext.User(Guid.NewGuid(), 007), ""));
                x.For<IDbConnection>().Use("Connection", s => new SqlConnection(s.GetInstance<IConnectionString>().ConnectionString));
                x.For<IDbTransaction>().Use("Transaction", s => s.GetInstance<IDbConnection>().BeginTransaction());
            });
        }

        public Container Container { get; private set; }
    }

    public interface IConnectionString
    {
        string ConnectionString { get; }
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