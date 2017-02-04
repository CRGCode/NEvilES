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
}