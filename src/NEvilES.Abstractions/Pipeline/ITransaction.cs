using System;

namespace NEvilES.Abstractions.Pipeline
{
    public interface ITransaction
    {
        Guid Id { get; }
    }

    public abstract class TransactionBase : ITransaction
    {
        public Guid Id { get; protected set; }
    }
}