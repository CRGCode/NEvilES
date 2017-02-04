using System;
using System.Linq;
using NEvilES.Pipeline;
using NEvilES.Tests.Sample;
using StructureMap;
using Xunit;

namespace NEvilES.Tests
{
    public class PipelineProcessorTests : IUseFixture<SharedFixtureContext>
    {
        private ICommandProcessor _commandProcessor;
        private IRepository _repository;
        private IContainer _container;
        
        public void SetFixture(SharedFixtureContext context)
        {
            _container = context.Container.GetNestedContainer();
            _commandProcessor = _container.GetInstance<ICommandProcessor>();
            _repository = _container.GetInstance<IRepository>();
        }

        [Fact]
        public void CommandWithDifferentEventHandlerOnAggregate()
        {
            var streamId = Guid.NewGuid();

            var expected = _commandProcessor.Process(new Employee.Create { StreamId = streamId, Person = new PersonalDetails("John","Smith")});
            Assert.Equal(streamId, expected.FilterEvents<Person.Created>().First().StreamId);
        }

        [Fact]
        public void CommandWithDifferentEventHandlerOnAggregateWithException()
        {
            var streamId = Guid.NewGuid();
            Assert.Throws<DomainAggregateException>(() => 
                _commandProcessor.Process(new Employee.Create { StreamId = streamId, Person = new PersonalDetails("John", "God") }));
        }

        [Fact]
        public void CommandWithHandlerDependencies()
        {
            var streamId = Guid.NewGuid();

            var netAmount = 60000M;
            _commandProcessor.Process(new Employee.Create { StreamId = streamId, Person = new PersonalDetails("John", "Smith") });
            var expected = _commandProcessor.Process(new Employee.PayPerson { StreamId = streamId, NetAmount = netAmount });
            var payPerson = expected.FilterEvents<Employee.PaidPerson>().First();
            Assert.Equal(streamId, payPerson.StreamId);
            Assert.True(payPerson.Tax < netAmount);
        }

        [Fact]
        public void CommandWithHandlerDependenciesResultingInAggregateStateChange()
        {
            var streamId = Guid.NewGuid();

            var bonus = 6000M;
            _commandProcessor.Process(new Employee.Create { StreamId = streamId, Person = new PersonalDetails("John", "Smith") });
            var expected = _commandProcessor.Process(new Employee.PayBonus { StreamId = streamId, Amount = bonus });
            var payPerson = expected.FilterEvents<Employee.PaidBonus>().First();
            Assert.Equal(streamId, payPerson.StreamId);

            var agg = _repository.Get<Employee.Aggregate>(streamId);
            Assert.Equal(bonus, agg.bonus);
        }

        [Fact]
        public void ProcessStatelessEvent()
        {
            var streamId = Guid.NewGuid();

            _commandProcessor.Process(new Employee.Create { StreamId = streamId, Person = new PersonalDetails("John", "Smith") });

            var expected = _commandProcessor.Process(new Person.StatelessBirthdateChanged { StreamId = streamId, Birthdate = DateTime.Now });
            Assert.Equal(streamId, expected.FilterEvents<Person.StatelessBirthdateChanged>().First().StreamId);
        }

        [Fact]
        public void WithProjector()
        {
            var streamId = Guid.NewGuid();

            var results = _commandProcessor.Process(new Employee.Create { StreamId = streamId, Person = new PersonalDetails("John", "Smith") });
            var projectedItem = results.FindProjectedItem<PersonalDetails>();
            Assert.True(projectedItem.FirstName == "John");

            results = _commandProcessor.Process(new Person.CorrectName { StreamId = streamId, Name = "New Name" });
            projectedItem = results.FindProjectedItem<PersonalDetails>();
            Assert.True(projectedItem.FirstName == "New");
        }

        [Fact]
        public void WithExternalValidator_Failure()
        {
            var readModel = _container.GetInstance<IReadModel>();
            readModel.People.Add(Guid.NewGuid(),new PersonalDetails("John", "Smith"));

            var streamId = Guid.NewGuid();

            Assert.Throws<CommandValidationException>(() => 
                _commandProcessor.Process(new Employee.Create { StreamId = streamId, Person = new PersonalDetails("John", "Smith") }));
        }

        [Fact]
        public void OneCommandToManyAggregates()
        {
            var streamId = Guid.NewGuid();

            var command = new Person.SendInvite (streamId, new PersonalDetails("John", "Smith"), "john@gmail.com");
            var expected = _commandProcessor.Process(command);

            Assert.True(expected.UpdatedAggregates.Count == 2);

            var projectedItem = expected.FindProjectedItem<PersonalDetails>();
            Assert.True(projectedItem.FirstName == command.Person.FirstName);

            var person = expected.FilterEvents<Person.Created>().First();
            Assert.True(person.Person.LastName == command.Person.LastName);
            var email = expected.FilterEvents<Email.PersonInvited>().First();
            Assert.True(email.StreamId != streamId);
            Assert.True(email.EmailAddress == command.Email);
        }
    }
}