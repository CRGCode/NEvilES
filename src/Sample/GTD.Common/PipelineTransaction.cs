// using System;
// using System.Data;
// using NEvilES;
// using NEvilES.Abstractions.Pipeline;
// using NEvilES.Pipeline;

// namespace GTD.Common
// {
//     public class PipelineTransaction : TransactionBase, IDisposable
//     {
//         private readonly IDbConnection connection;
//         private readonly IDbTransaction transaction;

//         public PipelineTransaction(IDbConnection connection, IDbTransaction transaction)
//         {
//             Id = CombGuid.NewGuid();
//             this.connection = connection;
//             this.transaction = transaction;
//         }

//         public void Dispose()
//         {
//             transaction.Commit();
//             connection.Close();
//         }
//     }
// }