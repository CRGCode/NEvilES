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
            agg.Raise<Customer.Created>(new Customer.Create()
            {
                StreamId = streamId,
                Name = "Chat 1"
            });
            var iAgg = (IAggregate) agg;
            Assert.Equal(1, iAgg.Version);
            Assert.Equal(streamId, iAgg.Id);
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
            agg.Raise<ChatRoom.Created>(new ChatRoom.Create { StreamId = streamId, Name = "Chat 1" });
            agg.RaiseStateless<ChatRoom.RoomRenamed>(new ChatRoom.RenameRoom()
            {
                StreamId = streamId,
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
            var ex = Assert.Throws<Exception>(() => agg.RaiseStateless<ChatRoom.RenameRoom>(new ChatRoom.RenameRoom()));
            Assert.Contains("You can't RaiseStateless<TEvent> where typeof(TEvent) is a Command", ex.Message);
        }

        [Fact]
        public void FailsWhenRaisingStatelessEventFromCommand_ButTheEventIsStateful_AsThereIs_ApplyMethodForTheEvent_()
        {
            var agg = new ChatRoom.Aggregate();
            var ex = Assert.Throws<Exception>(() => agg.RaiseStateless<ChatRoom.Created>(new ChatRoom.Create()));
            Assert.Contains("You can't RaiseStatelessEvent - There's a 'private void Apply", ex.Message);
        }

    }
}