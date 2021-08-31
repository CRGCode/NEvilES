using System;
using System.Data;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.SQL
{
    public class PipelineTransaction : TransactionBase, IDisposable
    {
        private readonly IDbConnection connection;
        private readonly IDbTransaction transaction;
        private bool rollback;

        public PipelineTransaction(IDbConnection connection, IDbTransaction transaction)
        {
            Id = CombGuid.NewGuid();
            this.connection = connection;
            this.transaction = transaction;
        }

        public void Dispose()
        {
            if (!rollback)
                transaction.Commit();
            connection.Close();
        }

        public override void Rollback()
        {
            transaction.Rollback();
            rollback = true;
        }
    }
}