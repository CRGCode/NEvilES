using System;
using NEvilES.Testing;
using Xunit;

namespace NEvilES.Tests
{
    using NEvilES.Abstractions;
    using NEvilES.Tests.CommonDomain.Sample;

    public class AggregateTests
    {
        [Fact]
        public void CanCreateNewSample()
        {
            var agg = new Customer.Aggregate();
            agg.Raise<Customer.Created>(new Customer.Create());
            var iAgg = (IAggregate) agg;
            Assert.Equal(iAgg.Version, 1);
        }
    }
}