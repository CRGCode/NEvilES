using System;

namespace NEvilES
{
    public class DomainAggregateException : Exception
    {
        public readonly AggregateBase Aggregate;

        public DomainAggregateException(AggregateBase aggregate, string message, params object[] args)
            : base(string.Format(message, args))
        {
            Aggregate = aggregate;
        }
    }

    public class AggregateOutOfDate : Exception
    {
        public readonly AggregateBase Aggregate;
        public AggregateOutOfDate(AggregateBase aggregate, string message)
            : base(message)
        {
            Aggregate = aggregate;
        }
        public AggregateOutOfDate(AggregateBase aggregate, string message, params object[] args)
            : base(string.Format(message, args))
        {
            Aggregate = aggregate;
        }
    }
}