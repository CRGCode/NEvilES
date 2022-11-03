using System;
using NEvilES.Abstractions;
using NEvilES.Testing;
using Xunit;
using NEvilES.Tests.CommonDomain.Sample;

namespace NEvilES.Tests
{

    public class BDD_StyleTests : BaseAggregateTest<Customer.Aggregate>
    {
        [Fact]
        public void CanCreateNewSample()
        {
            var streamId = Guid.NewGuid();
            var cmd = new Customer.Create  {CustomerId = streamId, Name = "Testing" };
            Test(Given(),
                When(x => x.Handle(cmd, new Customer.Validate())),
                Then(new Customer.Created {CustomerId = streamId, Name = cmd.Name}));
        }

        [Fact]
        public void NoNameFails_Create()
        {
            var cmd = new Customer.Create { CustomerId = Guid.NewGuid() };
            Test(Given(),
                When(x => x.Handle(cmd, new Customer.Validate())),
                ThenFailWith<DomainAggregateException>());
        }

        [Fact]
        public void FailsExpectedEvent()
        {
            var streamId = Guid.NewGuid();
            Test(Given(new Customer.Created { CustomerId = streamId, Name = "Customer 1" }),
                When(x => x.Handle(new Customer.Complain { CustomerId = streamId, Reason = "Not Happy" })),
                Then(new Customer.Complaint { CustomerId = streamId, Reason = "Not Happy" },
                    new Customer.NoteAdded { CustomerId = streamId, Text = "Not Happy" }
                    ));
        }
    }

    public class BDD_StyleTests_Person : BaseAggregateTest<Person.Aggregate>
    {
        [Fact]
        public void CanCorrectName()
        {
            var streamId = Guid.NewGuid();
            Test(Given(
                    new Person.Created(streamId, new PersonalDetails("John", "Smith")),
                    new Person.NameCorrected { PersonId = streamId, Name = "CraigGardiner" }),
                When(x => x.Handle(new Person.CorrectName { PersonId = streamId, Name = "Craig Gardiner" })),
                Then(new Person.NameCorrectedV2 { PersonId = streamId, FirstName = "Craig", LastName = "Gardiner" }));
        }

        [Fact]
        public void CanAddComment()
        {
            var streamId = Guid.NewGuid();
            Test(Given(
                    new Person.Created(streamId, new PersonalDetails("John", "Smith"))),
                When(x => x.Handle(new Person.AddComment { PersonId = streamId, Comment = "Blah BLAH!!" })),
                Then(new Person.CommentAdded { PersonId = streamId, Comment = "Blah BLAH!!" }));
        }
    }
}