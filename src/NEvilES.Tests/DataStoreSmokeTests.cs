using System;
using System.Collections.Generic;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.Tests
{
    public class Transaction : ITransaction
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
            Assert.Equal(0, expected.Version);
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
            Assert.Single(expected.UpdatedEvents);
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
            Assert.Equal(2, expected.UpdatedEvents.Length);
        }



        [Fact]
        public void CheckAggregateApplysEvent()
        {

            var streamId = Guid.NewGuid();

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();
            var user3 = Guid.NewGuid();


            var agg = new ChatRoom.Aggregate();
            agg.Handle(new ChatRoom.Create() { StreamId = streamId, Name = "Bobs Chat", InitialUsers = new HashSet<Guid> { user1 } });
            agg.Handle(new ChatRoom.IncludeUserInRoom() { StreamId = streamId, UserId = user2 });
            agg.Handle(new ChatRoom.IncludeUserInRoom() { StreamId = streamId, UserId = user3 });

            var expected = repository.Save(agg);
            Assert.NotNull(expected);
            Assert.Equal(streamId, expected.StreamId);
            Assert.Equal(3, expected.UpdatedEvents.Length);
            Assert.Equal(3, agg.Version);
        }

        // [Fact]
        // public void CommandWithDifferentEventHandlerOnAggregateWithException()
        // {
        //    var streamId = Guid.NewGuid();
        //    Assert.Throws<DomainAggregateException>(() =>
        //        _commandProcessor.Process(new Employee.Create
        //        {
        //            StreamId = streamId,
        //            Person = new PersonalDetails("John", "God")
        //        }));
        // }
    }
}