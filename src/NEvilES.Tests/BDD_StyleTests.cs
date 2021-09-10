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
            var cmd = new Customer.Create() {StreamId = streamId, Name = "Testing"};
            Test(Given(),
                When(x => x.Handle(cmd, new Customer.Validate())),
                Then(new Customer.Created {StreamId = streamId, Name = cmd.Name}));
        }

        [Fact]
        public void NoNameFails_Create()
        {
            var cmd = new Customer.Create { StreamId = Guid.NewGuid() };
            Test(Given(),
                When(x => x.Handle(cmd, new Customer.Validate())),
                ThenFailWith<DomainAggregateException>());
        }

        [RunnableInDebugOnly]
        public void FailsExpectedEvent()
        {
            var streamId = Guid.NewGuid();
            Test(Given(new Customer.Created { StreamId = streamId, Name = "Customer 1" }),
                When(x => x.Handle(new Customer.Complain() { StreamId = streamId, Reason = "Not Happy" })),
                Then(new Customer.Complaint { StreamId = streamId, Reason = "Not Happy" }));
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
                    new Person.NameCorrected { StreamId = streamId, Name = "CraigGardiner" }),
                When(x => x.Handle(new Person.CorrectName { StreamId = streamId, Name = "Craig Gardiner" })),
                Then(new Person.NameCorrectedV2 { StreamId = streamId, FirstName = "Craig", LastName = "Gardiner" }));
        }

        [Fact]
        public void CanAddComment()
        {
            var streamId = Guid.NewGuid();
            Test(Given(
                    new Person.Created(streamId, new PersonalDetails("John", "Smith"))),
                When(x => x.Handle(new Person.AddComment { StreamId = streamId, Comment = "Blah BLAH!!" })),
                Then(new Person.CommentAdded { StreamId = streamId, Comment = "Blah BLAH!!" }));
        }
    }
}