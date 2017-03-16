using System;

namespace NEvilES.Pipeline
{
    public class CommandContext
    {
        public CommandContext(IUser by, ITransaction transaction, IUser impersonatorBy, string appVersion)
        {
            By = by;
            Transaction = transaction;
            ImpersonatorBy = impersonatorBy;
            AppVersion = appVersion;
            Result = new CommandResult();
        }

        public IUser By { get; set; }
        public ITransaction Transaction { get; set; }
        public IUser ImpersonatorBy { get; set; }
        public string AppVersion { get; set; }
        public CommandResult Result { get; set; }
        public ApprovalContext ApprovalContext { get; set; }

        public class User : IUser
        {
            public User(Guid id)
            {
                GuidId = id;
            }

            public User(Guid id, int user)
            {
                GuidId = id;
                UserId = user != 0 ? (int?)user : null;
            }

            public Guid GuidId { get; private set; }
            public int? UserId { get; set; }
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

    public class ApprovalContext
    {
        public enum Action
        {
            Request,
            Approve,
            Decline
        }

        public Action Perform { get; }
        public string Reason { get; set; }

        public ApprovalContext(Action perform)
        {
            Perform = perform;
        }
    }
}