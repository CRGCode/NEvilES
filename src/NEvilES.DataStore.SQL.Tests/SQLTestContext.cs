using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.DataStore.MSSQL;
using Xunit.Abstractions;

namespace NEvilES.DataStore.SQL.Tests
{
    public class SQLTestContext : BaseTestContext
    {
        public SQLTestContext() : base("Server=AF-004;Database=ES_Test;Trusted_Connection=True")
        {
        }

        protected override void AddServices(IServiceCollection services)
        {
            services.AddScoped<IDbConnection>(c =>
            {
                var conn = new SqlConnection(c.GetRequiredService<IConnectionString>().Data);
                conn.Open();
                return conn;
            });

            services.AddAllGenericTypes(typeof(IWriteReadModel<>), new[] { typeof(SQLDocumentRepository<>).Assembly });
            services.AddAllGenericTypes(typeof(IReadFromReadModel<>), new[] { typeof(SQLDocumentRepository<>).Assembly });
            
            new MSSQLEventStoreCreate().CreateOrWipeDb(new ConnectionString(ConnString));
        }
    }
}