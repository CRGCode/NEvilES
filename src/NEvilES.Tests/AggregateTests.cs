using Xunit;

namespace NEvilES.Tests
{
    using Abstractions;
    using CommonDomain.Sample;

    public class AggregateTests
    {
        [Fact]
        public void CanCreateNewSample()
        {
            var agg = new Customer.Aggregate();
            agg.Raise<Customer.Created>(new Customer.Create());
            var iAgg = (IAggregate) agg;
            Assert.Equal(1, iAgg.Version);
        }
    }
}