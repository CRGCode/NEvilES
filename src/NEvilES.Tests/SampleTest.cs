using System;
using NEvilES.Testing;
using Xunit;

namespace NEvilES.Tests
{
    using Sample;

    public class SampleTest : BaseAggregateTest<Customer.Aggregate>
    {
        //[Fact]
        //public void CanCreateNewSample()
        //{
        //    var streamId = Guid.NewGuid();
        //    var cmd = new Customer.Create() {StreamId = streamId, Person = new PersonalDetails("John", "Smith")};
        //    Test(Given(),
        //        When(x => x.Handle(cmd)),
        //        Then(new Person.Created(streamId, cmd.Person)));
        //}

        [Fact]
        public void CanCorrectName()
        {
            var streamId = Guid.NewGuid();
            Test(Given(
                    new Person.Created(streamId, new PersonalDetails("John", "Smith")),
                    new Person.NameCorrected() {StreamId = streamId, Name = "CraigGardiner"}),
                When(x => x.Handle(new Person.CorrectName() {StreamId = streamId, Name = "Craig Gardiner"})),
                Then(new Person.NameCorrectedV2 {StreamId = streamId, FirstName = "Craig", LastName = "Gardiner"}));
        }

        [Fact]
        public void NoNameFails_Create()
        {
            var cmd = new Customer.Create {StreamId = Guid.NewGuid(), Person = new PersonalDetails("Test", "God")};
            Test(Given(),
                When(x => x.Handle(cmd)),
                ThenFailWith<DomainAggregateException>());
        }
    }
}