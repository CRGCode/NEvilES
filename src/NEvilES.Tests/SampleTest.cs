using System;
using NEvilES.Testing;
using Xunit;

namespace NEvilES.Tests
{
    public class SampleTest : BaseAggregateTest<SampleAggregate>
    {
        [Fact]
        public void CanCreateNewSample()
        {
            var streamId = Guid.NewGuid();
            var cmd = new Sample.Create() { StreamId = streamId, Person = new Sample.Person("John", "Smith") };
            Test(Given(),
                When(cmd),
                Then(new Sample.Created { StreamId = streamId, Person = cmd.Person }));
        }

        [Fact]
        public void CanCorrectName()
        {
            var streamId = Guid.NewGuid();
            Test(Given(
                new Sample.Created { StreamId = streamId, Person = new Sample.Person("John", "Smith") },
                new Sample.NameCorrected() {StreamId = streamId, Name = "CraigGardiner"}),
                When(x => x.CorrectName(new Sample.CorrectName() { StreamId = streamId, Name = "Craig Gardiner" })),
                Then(new Sample.NameCorrectedV2 { StreamId = streamId, FirstName = "Craig", LastName = "Gardiner"}));
        }

        [Fact]
        public void CanCalculateTax()
        {
            var streamId = Guid.NewGuid();
            var cmd = new Sample.CalculateTax() { StreamId = streamId };
            Test(Given(),
                When(sut => sut.CalculateTax(cmd, new Sample.TaxRuleEngine())),
                Then(new Sample.TaxCalculated() { StreamId = streamId }));
        }

        [Fact]
        public void NoNameFails_Create()
        {
            var cmd = new Sample.Create() { StreamId = Guid.NewGuid() };
            Test(Given(),
                When(cmd),
                ThenFailWith<DomainAggregateException>());
        }

    }
}