using System;
using System.Data;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.SQL
{
    public class PipelineTransaction : TransactionBase, IDisposable
    {
        private readonly IDbConnection connection;
        public IDbTransaction Transaction { get; }
        private bool rollback;
        private bool disposed;

        public PipelineTransaction(IDbConnection connection, IDbTransaction transaction)
        {
            Id = CombGuid.NewGuid();
            this.connection = connection;
            Transaction = transaction;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) 
                return;

            if (disposing)
            {
                if (!rollback)
                    Transaction.Commit();
                Transaction.Dispose();
                connection.Close();
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public override void Rollback()
        {
            Transaction.Rollback();
            rollback = true;
        }
    }
}