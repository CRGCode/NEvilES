using System;

namespace NEvilES.Abstractions.Pipeline
{
    public interface ITransaction
    {
        Guid Id { get; }
        void Rollback();
    }

    public abstract class TransactionBase : ITransaction
    {
        public Guid Id { get; protected set; }
        public abstract void Rollback();
    }
}