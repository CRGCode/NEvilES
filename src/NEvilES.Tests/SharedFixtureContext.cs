using System;
using System.Collections;
using System.Collections.Generic;
using NEvilES.Pipeline;
using NEvilES.Tests.Sample;
using StructureMap;

namespace NEvilES.Tests
{
    public class SharedFixtureContext
    {
        public SharedFixtureContext()
        {
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

                x.For<ICommandProcessor>().Use<PipelineProcessor>();
                x.For<IRepository>().Use<InMemoryEventStore>();
                x.For<IReadModel>().Use<TestReadModel>();

                x.For<CommandContext>().Use("CommandContext", s => new CommandContext(new CommandContext.User(Guid.NewGuid(), 666), Guid.NewGuid(), Guid.NewGuid(), new CommandContext.User(Guid.NewGuid(), 007), ""));
            });
        }


        public Container Container { get; private set; }
    }

    public class TestReadModel : IReadModel
    {
        public TestReadModel()
        {
            People = new Dictionary<Guid, PersonalDetails>();
        }

        public Dictionary<Guid, PersonalDetails> People { get; }
    }

    public class Factory : IFactory
    {
        private readonly IContainer _container;

        public Factory(IContainer container)
        {
            _container = container;
        }

        public object Get(Type type)
        {
            return _container.GetInstance(type);
        }

        public object TryGet(Type type)
        {
            return _container.TryGetInstance(type);
        }

        public IEnumerable GetAll(Type type)
        {
            return _container.GetAllInstances(type);
        }
    }

}