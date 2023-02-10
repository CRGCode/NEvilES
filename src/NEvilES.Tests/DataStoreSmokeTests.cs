using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.Tests
{
    [Collection("Serial")]
    public class DataStoreSmokeTests : IClassFixture<SharedFixtureContext>, IDisposable
    {
        private readonly IRepository repository;
        private readonly IServiceScope scope;

        public DataStoreSmokeTests(SharedFixtureContext context, ITestOutputHelper output)
        {
            context.OutputHelper = output;
            scope = context.Container.CreateScope();
            repository = scope.ServiceProvider.GetRequiredService<IRepository>();
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
            agg.Handle(new Customer.Create { CustomerId = streamId, Details = new PersonalDetails("Test","Last") }, new Customer.Validate());

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
            agg.Handle(new Customer.Create { CustomerId = streamId, Details = new PersonalDetails("Test","Last") }, new Customer.Validate());
            agg.RaiseStatelessEvent(new Customer.Refunded{CustomerId = streamId, Amount  = 800.80M});

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
            agg.Handle(new ChatRoom.Create { ChatRoomId = streamId, Name = "Bobs Chat", InitialUsers = new HashSet<Guid> { user1 } });
            agg.Handle(new ChatRoom.IncludeUserInRoom { ChatRoomId = streamId, UserId = user2 });
            agg.Handle(new ChatRoom.IncludeUserInRoom { ChatRoomId = streamId, UserId = user3 });

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
        //        _commandProcessor.ProcessWithRetry(new Employee.Create
        //        {
        //            Id = streamId,
        //            Details = new PersonalDetails("John", "God")
        //        }));
        // }
        public void Dispose()
        {
            scope?.Dispose();
        }
    }
}