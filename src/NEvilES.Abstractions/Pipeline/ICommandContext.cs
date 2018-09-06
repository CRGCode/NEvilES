using System;

namespace NEvilES.Abstractions.Pipeline
{

    public interface ICommandContext
    {
        IUser By { get; set; }
        ITransaction Transaction { get; set; }
        IUser ImpersonatorBy { get; set; }
        string AppVersion { get; set; }
        ICommandResult Result { get; set; }

    }


    public interface IUser
    {
        Guid GuidId { get; }
    }

    public abstract class TransactionBase : ITransaction
    {
        public Guid Id { get; protected set; }
    }

    public interface ITransaction
    {
        Guid Id { get; }
    }
}