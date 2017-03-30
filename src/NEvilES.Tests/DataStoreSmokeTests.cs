using System;
using System.Data;
using System.Diagnostics;
using NEvilES.DataStore;
using NEvilES.Pipeline;
using NEvilES.Tests.Sample;
using StructureMap;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.Tests
{
    public class Transaction : CommandContext.ITransaction
    {
        public Guid Id { get; }
        public Transaction(Guid id)
        {
            Id = id;
        }
    }

    public class DataStoreSmokeTests : IClassFixture<SharedFixtureContext>
    {
        //This is use instead of Console.Write or Debug.Write
        private readonly ITestOutputHelper output;
        private readonly IRepository repository;

        public DataStoreSmokeTests(ITestOutputHelper helper, SharedFixtureContext context)
        {
            output = helper;
            repository = context.Container.GetInstance<IRepository>();
        }

        [Fact]
        public void Get_NoEvents()
        {
            var streamId = Guid.NewGuid();

            var expected = repository.Get<Customer.Aggregate>(streamId);
            Assert.NotNull(expected);
            Assert.Equal(expected.Id, streamId);
            Assert.Equal(expected.Version, 0);
        }

        [Fact]
        public void Save_Events()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Handle(new Customer.Create { StreamId = streamId, Name = "Test" }, new Customer.Validate());

            var expected = repository.Save(agg);
            Assert.NotNull(expected);
            Assert.Equal(expected.StreamId, streamId);
            Assert.Equal(expected.UpdatedEvents.Length, 1);
        }

         [Fact]
        public void Save_Events_Stateless()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Handle(new Customer.Create { StreamId = streamId, Name = "Test" }, new Customer.Validate());
            agg.RaiseStateless(new Customer.Refunded(streamId, 800.80M));

            var expected = repository.Save(agg);
            Assert.NotNull(expected);
            Assert.Equal(expected.StreamId, streamId);
            Assert.Equal(expected.UpdatedEvents.Length, 2);
        }

        //[Fact]
        //public void CommandWithDifferentEventHandlerOnAggregateWithException()
        //{
        //    var streamId = Guid.NewGuid();
        //    Assert.Throws<DomainAggregateException>(() =>
        //        _commandProcessor.Process(new Employee.Create
        //        {
        //            StreamId = streamId,
        //            Person = new PersonalDetails("John", "God")
        //        }));
        //}
    }
}