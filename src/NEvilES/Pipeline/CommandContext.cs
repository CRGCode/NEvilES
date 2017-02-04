using System;

namespace NEvilES.Pipeline
{
    public class CommandContext
    {
        public CommandContext(User by, Guid transactionId, Guid? sessionId, User impersonatorBy, string appVersion)
        {
            By = by;
            TransactionId = transactionId;
            ImpersonatorBy = impersonatorBy;
            AppVersion = appVersion;
            SessionId = sessionId;
            Result = new CommandResult();
        }

        public User By { get; set; }
        public Guid TransactionId { get; set; }
        public User ImpersonatorBy { get; set; }
        public string AppVersion { get; set; }
        public Guid? SessionId { get; set; }
        public CommandResult Result { get; set; }

        public class User
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
    }
}