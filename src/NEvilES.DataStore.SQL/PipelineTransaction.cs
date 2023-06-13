using System;
using System.Data;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.SQL
{
    public class PipelineTransaction : TransactionBase, IDisposable
    {
        private readonly ILogger<PipelineTransaction> log;
        private readonly IDbConnection connection;
        public IDbTransaction Transaction { get; }
        private bool rollback;
        private bool disposed;

        public PipelineTransaction(ILogger<PipelineTransaction> logger, IDbConnection connection, IDbTransaction transaction)
        {
            Id = CombGuid.NewGuid();
            log = logger;
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
                {
                    Transaction.Commit();
                    log.LogDebug($"Transaction {Id} Committed");
                }

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
            log.LogDebug($"Transaction {Id} Rollback");

            rollback = true;
        }
    }
}