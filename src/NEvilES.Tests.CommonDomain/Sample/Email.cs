using System;
using NEvilES.Abstractions;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class Email
    {
        public class PersonInvited : IEvent
        {
            public Guid StreamId { get; set; }
            public string EmailAddress { get; set; }
            public Guid GetStreamId() => StreamId;
        }

        public class Aggregate : StatelessAggregate,
            IHandleStatelessEvent<PersonInvited>
        {
        }
    }
}