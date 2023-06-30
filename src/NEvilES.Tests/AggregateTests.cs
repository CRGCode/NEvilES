using System;
using Xunit;

namespace NEvilES.Tests
{
    using Abstractions;
    using CommonDomain.Sample;

    public class AggregateTests
    {
        [Fact]
        public void CanRaiseEvent()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Raise<Customer.Created>(new Customer.Create
            {
                CustomerId = streamId,
                Details = new PersonalDetails("Chat","Last")
            });
            var iAgg = (IAggregate) agg;

            Assert.Equal(1, iAgg.Version);
            Assert.Equal(streamId, iAgg.Id);
        }

        [Fact]
        public void RaiseEventOrdering()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Raise<Customer.Created>(new Customer.Create
            {
                CustomerId = streamId,
                Details = new PersonalDetails("Chat","Last")
            });
            agg.RaiseStatelessEvent(new Customer.Refunded{CustomerId = streamId,Amount = 600});

            var iAgg = (IAggregate)agg;

            Assert.Equal(streamId, iAgg.Id);
            Assert.Equal(2, iAgg.Version);
            var events = new object[2];
            iAgg.GetUncommittedEvents().CopyTo(events, 0);
            Assert.Equal(600,  ((Customer.Refunded)((EventData)events[1]).Event).Amount);
        }

        //[Fact]
        //public void FailRaiseStatelessEvent_NoStream() // Not sure about this, do we need some sort if marker for the first Event?
        //{
        //    var streamId = Guid.NewGuid();
        //    var agg = new Customer.Aggregate();
        //    agg.RaiseStatelessEvent(new Customer.Refunded(streamId, 100));
        //    var iAgg = (IAggregate)agg;
        //    Assert.Equal(1, iAgg.Version);
        //    Assert.Equal(streamId, iAgg.Id);
        //}

        [Fact]
        public void CanRaiseStatelessEventFromCommand()
        {
            var streamId = Guid.NewGuid();
            var agg = new ChatRoom.Aggregate();
            agg.Raise<ChatRoom.Created>(new ChatRoom.Create { ChatRoomId = streamId, Name = "Chat 1" });
            agg.RaiseStateless<ChatRoom.RoomRenamed>(new ChatRoom.RenameRoom
            {
                ChatRoomId = streamId,
                NewName = "Chat 1"
            });
           var iAgg = (IAggregate)agg;
           Assert.Equal(2, iAgg.Version);
           Assert.Equal(streamId, iAgg.Id);
        }

        [Fact]
        public void FailsWhenRaisingStatelessEventFromCommand()
        {
            var agg = new ChatRoom.Aggregate();
            var ex = Assert.Throws<Exception>(() => agg.RaiseStateless<ChatRoom.RoomRenamed>(new ChatRoom.RenameRoom()));
            Assert.Contains("You can't RaiseStateless<TEvent> where typeof(TEvent) is a Command", ex.Message);
        }

        [Fact]
        public void FailsWhenRaisingStatelessEventFromCommand_ButTheEventIsStateful_AsThereIs_ApplyMethodForTheEvent_()
        {
            var agg = new ChatRoom.Aggregate();
            var ex = Assert.Throws<Exception>(() => agg.RaiseStateless<ChatRoom.Created>(new ChatRoom.Create()));
            Assert.Contains("You can't RaiseStatelessEvent - There's a 'private void Apply", ex.Message);
        }

        [Fact]
        public void CanHandleCommand()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Raise<Customer.Created>(new Customer.Create
            {
                CustomerId = streamId,
                Details = new PersonalDetails("Customer", "One")
            });
            agg.Handle(new Customer.Complain { CustomerId = streamId, Reason = "Not Happy"});
            var iAgg = (IAggregate)agg;
            Assert.Equal(3, iAgg.Version);
            Assert.Equal(streamId, iAgg.Id);
        }
    }
}