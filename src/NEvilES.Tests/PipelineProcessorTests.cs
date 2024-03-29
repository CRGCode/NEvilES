using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;
using NEvilES.Tests.CommonDomain.Sample;
using NEvilES.Tests.CommonDomain.Sample.ReadModel;
using Xunit;
using Xunit.Abstractions;
using ChatRoom = NEvilES.Tests.CommonDomain.Sample.ReadModel.ChatRoom;
using Customer = NEvilES.Tests.CommonDomain.Sample.Customer;

namespace NEvilES.Tests
{
    [Collection("Serial")]
    public class PipelineProcessorTests : IClassFixture<SharedFixtureContext>, IDisposable
    {
        private readonly PipelineProcessor pipelineProcessor;
        private readonly IRepository repository;
        private readonly IServiceScope scope;

        public PipelineProcessorTests(SharedFixtureContext context, ITestOutputHelper output)
        {
            context.OutputHelper = output;
            scope = context.Container.CreateScope();
            pipelineProcessor = scope.ServiceProvider.GetRequiredService<PipelineProcessor>();
            repository = scope.ServiceProvider.GetRequiredService<IRepository>();
        }

        [Fact]
        public void CommandWithSeparateHandler()
        {
            var streamId = Guid.NewGuid();

            var expected = pipelineProcessor.Process(new Customer.SendInvite(streamId, new PersonalDetails("John", $"Smith{streamId}"),""));
            Assert.Equal(streamId, expected.FilterEvents<Customer.Created>().First().CustomerId);
        }

        [Fact]
        public void CommandWithDifferentEventHandlerOnAggregate()
        {
            var streamId = Guid.NewGuid();

            var expected = pipelineProcessor.Process(new Employee.Create { PersonId = streamId, Person = new PersonalDetails("John", $"Smith{streamId}") });
            Assert.Equal(streamId, expected.FilterEvents<Person.Created>().First().PersonId);
        }


        [Fact]
        public void CommandWithDifferentEventHandlerOnAggregateWithException()
        {
            var streamId = Guid.NewGuid();

            Assert.Throws<DomainAggregateException>(() =>
                pipelineProcessor.Process(new Employee.Create { PersonId = streamId, Person = new PersonalDetails("John", "God") }));
        }

        [Fact]
        public void CommandWithHandlerDependencies()
        {
            var streamId = Guid.NewGuid();

            var netAmount = 60000M;
            pipelineProcessor.Process(new Employee.Create { PersonId = streamId, Person = new PersonalDetails("John", $"Smith{streamId}") });
            var expected = pipelineProcessor.Process(new Employee.PayPerson { EmployeeId = streamId, NetAmount = netAmount });
            var payPerson = expected.FilterEvents<Employee.PaidPerson>().First();
            Assert.Equal(streamId, payPerson.EmployeeId);
            Assert.True(payPerson.Tax < netAmount);
        }


        [Fact]
        public void CommandThatThrows_DomainAggregateException()
        {
            var streamId = Guid.NewGuid();

            pipelineProcessor.Process(new Employee.Create { PersonId = streamId, Person = new PersonalDetails("John", $"Smith{streamId}") });
            var expected = Assert.Throws<DomainAggregateException>(() => pipelineProcessor.Process(new Employee.BadNews { EmployeeId = streamId }));
            Assert.Contains("Bad", expected.Message);
        }

        [Fact]
        public void CommandWithMissingHandlerDependency()
        {
            var streamId = Guid.NewGuid();

            pipelineProcessor.Process(new Employee.Create { PersonId = streamId, Person = new PersonalDetails("John", $"Smith{streamId}") });

            var expected = Assert.Throws<MissingHandlerDependency>(() => pipelineProcessor.Process(new Employee.DoNothing() { EmployeeId = streamId }));

            Assert.Contains("DoNothing", expected.Message);
        }

        [Fact]
        public void CommandWithHandlerDependenciesResultingInAggregateStateChange()
        {
            var streamId = Guid.NewGuid();

            var bonus = 6000M;
            pipelineProcessor.Process(new Employee.Create { PersonId = streamId, Person = new PersonalDetails("John", $"Smith{streamId}") });

            var expected = pipelineProcessor.Process(new Employee.PayBonus { EmployeeId = streamId, Amount = bonus });
            var payPerson = expected.FilterEvents<Employee.BonusPaid>().First();
            Assert.Equal(streamId, payPerson.EmployeeId);
            Assert.Equal(bonus, payPerson.Amount);

            var agg = repository.Get<Employee.Aggregate>(streamId);
            Assert.Equal(bonus, agg.Bonus);
        }

        [Fact]
        public void CommandRaises2Events()
        {
            var streamId = Guid.NewGuid();
            pipelineProcessor.Process(new Customer.Create() { CustomerId = streamId, Details = new PersonalDetails("John","Smith") });

            const string reason = "Some reason for complaining";
            var expected = pipelineProcessor.Process(new Customer.Complain{ CustomerId = streamId, Reason = reason});
            Assert.Equal(reason, expected.FilterEvents<Customer.Complaint>().First().Reason);
            Assert.Equal(reason, expected.FilterEvents<Customer.NoteAdded>().First().Text);

            var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<Guid>>();
            var customer = reader.Get<CommonDomain.Sample.ReadModel.Customer>(streamId);
            Assert.Equal(reason, customer.Complaints.First());
            Assert.Equal(reason, customer.Notes.First());
        }

        [Fact]
        public void ProcessStatelessEvent()
        {
            var streamId = Guid.NewGuid();

            pipelineProcessor.Process(new Employee.Create { PersonId = streamId, Person = new PersonalDetails("John", "Smith") });

            var expected = pipelineProcessor.Process(new Person.StatelessBirthdateChanged { PersonId = streamId, Birthdate = DateTime.Now });
            Assert.Equal(streamId, expected.FilterEvents<Person.StatelessBirthdateChanged>().First().PersonId);
        }

        [Fact]
        public void ProcessPatch()
        {
            var streamId = Guid.NewGuid();

            pipelineProcessor.Process(new CommonDomain.Sample.ChatRoom.Create() { Name = "Chatter Box", ChatRoomId = streamId, State = "VIC"});

            var expected = pipelineProcessor.Process(new PatchEvent(streamId, "Location.State", "NSW"));
            Assert.Equal(streamId, expected.FilterEvents<PatchEvent>().First().GetStreamId());
            var chatRoom = expected.FindProjectedItem<ChatRoom>();
            Assert.Equal(streamId, expected.FilterEvents<PatchEvent>().First().GetStreamId());
        }


        [Fact]
        public void BadProcessStatelessEvent_Throws()
        {
            var streamId = Guid.NewGuid();

            Assert.Throws<Exception>(() =>
                pipelineProcessor.Process(new Customer.BadStatelessEvent { CustomerId = streamId }));
        }

        [Fact]
        public void WithProjector()
        {
            var streamId = Guid.NewGuid();

            var results = pipelineProcessor.Process(new Employee.Create()
            {
                PersonId = streamId,
                Person = new PersonalDetails("John", $"Smith{streamId}")
            });
            PersonalDetails projectedItem = results.FindProjectedItem<PersonReadModel>();
            Assert.True(projectedItem.FirstName == "John");

            results = pipelineProcessor.Process(new Person.CorrectName { PersonId = streamId, Name = "New Name" });
            projectedItem = results.FindProjectedItem<PersonalDetails>();
            Assert.True(projectedItem.FirstName == "New");
        }

        [Fact]
        public void WithExternalValidator_Failure()
        {
            var readModel = scope.ServiceProvider.GetRequiredService<DocumentStoreGuid>();
            var personId = Guid.NewGuid();
            var model = new PersonReadModel(personId, "John", "Smith");
            readModel.Insert(model);

            Assert.Throws<CommandValidationException>(() =>
                // below doesn't work because we don't register handlers for sub-types Employee descends from Details -> We only register INeedExternal
                //pipelineProcessor.Process(new Details.Create { PersonId = personId, Details = new PersonalDetails("John", "Smith") }));
                pipelineProcessor.Process(new Employee.Create { PersonId = personId, Person = model }));
        }

        [Fact] 
        public void OneCommandToManyAggregates()
        {
            var streamId = Guid.NewGuid();

            var command = new Customer.SendInvite(streamId, new PersonalDetails("John", $"Smith+{Guid.NewGuid()}"), "john@gmail.com");
            var expected = pipelineProcessor.Process(command);

            Assert.Equal(2, expected.UpdatedAggregates.SelectMany(x => x.UpdatedEvents).Count());

            var projectedItem = expected.FindProjectedItem<PersonalDetails>();
            Assert.True(projectedItem.FirstName == command.Details.FirstName);

            var person = expected.FilterEvents<Customer.Created>().First();
            Assert.True(person.Details.LastName == command.Details.LastName);
            var email = expected.FilterEvents<Email.PersonInvited>().First();
            Assert.True(email.StreamId == streamId);
            Assert.True(email.EmailAddress == command.Email);
        }

        [Fact]
        public void Projector_RaiseCommandCastAsEvent_GivenCommandInheritsFromEvent()
        {
            var streamId = Guid.NewGuid();
            pipelineProcessor.Process(new Employee.Create { PersonId = streamId, Person = new PersonalDetails("John", $"Smith{streamId}") });
            var bonus = new Employee.PayBonus { EmployeeId = streamId, Amount = 10000M };
            var results = pipelineProcessor.Process(bonus);
            
            var projectedItem = (decimal)results.ReadModelItems.First(x => x is decimal);

            Assert.True(projectedItem == bonus.Amount);
        }

        [Fact]
        public void Projector_RaiseStatelessEvent()
        {
            var streamId = Guid.NewGuid();

            var email = new Customer.EmailSent { CustomerId = streamId, Text = "Testing" };
            var results = pipelineProcessor.Process(email);
            var projectedItem = results.ReadModelItems[0];
            Assert.True((string)projectedItem == email.Text);
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}