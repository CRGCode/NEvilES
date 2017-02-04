using System;

namespace NEvilES.Tests.Sample
{
    public class Customer
    {
        public class Refunded : IEvent
        {
            public Guid StreamId { get; set; }
            public decimal Amount { get; set; }

            public Refunded(Guid streamId, decimal amount)
            {
                StreamId = streamId;
                Amount = amount;
            }
        }

        public class Create : Person.Create
        {
        }

        public class Aggregate : Person.Aggregate,
            IHandleAggregateCommand<Create>,
            IHandleStatelessEvent<Refunded>
        {
            public void Handle(Create command)
            {
                base.Handle(command);
            }
        }
    }
}
