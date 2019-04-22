using System;
using NEvilES.Abstractions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Tests.CommonDomain.Sample;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NEvilES.DataStore.DynamoDB.Tests
{
    public class DynamoDBSmokeTests : IClassFixture<TestContext>
    {
        private readonly IAsyncRepository repository;

        public DynamoDBSmokeTests(TestContext context)
        {
            this.repository = context.Services.GetService<IAsyncRepository>();
        }

        [Fact]
        public async Task Get_NoEventsAsync()
        {
            var streamId = Guid.NewGuid();

            var expected = await repository.GetAsync<Customer.Aggregate>(streamId);
            Assert.NotNull(expected);
            Assert.Equal(expected.Id, streamId);
            Assert.Equal(expected.Version, 0);
        }

        [Fact]
        public async Task Save_EventsAsync()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Handle(new Customer.Create { StreamId = streamId, Name = "Test" }, new Customer.Validate());

            var expected = await repository.SaveAsync(agg);
            Assert.NotNull(expected);
            Assert.Equal(expected.StreamId, streamId);
            Assert.Equal(expected.UpdatedEvents.Length, 1);
        }

        [Fact]
        public async Task Save_Events_StatelessAsync()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Handle(new Customer.Create { StreamId = streamId, Name = "Test" }, new Customer.Validate());
            agg.RaiseStateless(new Customer.Refunded(streamId, 800.80M));

            var expected = await repository.SaveAsync(agg);
            Assert.NotNull(expected);
            Assert.Equal(expected.StreamId, streamId);
            Assert.Equal(expected.UpdatedEvents.Length, 2);
        }

        [Fact]
        public async Task Get_Events_StatelessAsync()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Handle(new Customer.Create { StreamId = streamId, Name = "Test" }, new Customer.Validate());
            agg.RaiseStateless(new Customer.Refunded(streamId, 800.80M));

            var res = await repository.SaveAsync(agg);
            Assert.Equal(res.StreamId, streamId);
            Assert.Equal(res.UpdatedEvents.Length, 2);

            var expected = await repository.GetStatelessAsync(typeof(Customer.Aggregate), streamId);
            Assert.NotNull(expected);
            Assert.Equal(2, expected.Version);
        }



        [Fact]
        public async Task CheckAggregateApplysEventAsync()
        {

            var streamId = Guid.NewGuid();

            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();
            var user3 = Guid.NewGuid();


            var agg = new ChatRoom.Aggregate();
            agg.Handle(new ChatRoom.Create() { StreamId = streamId, Name = "Bobs Chat", InitialUsers = new HashSet<Guid> { user1 } });
            agg.Handle(new ChatRoom.IncludeUserInRoom() { StreamId = streamId, UserId = user2 });
            agg.Handle(new ChatRoom.IncludeUserInRoom() { StreamId = streamId, UserId = user3 });

            var expected = await repository.SaveAsync(agg);
            Assert.NotNull(expected);
            Assert.Equal(streamId, expected.StreamId);
            Assert.Equal(3, expected.UpdatedEvents.Length);
            Assert.Equal(3, agg.Version);
        }

    }
}