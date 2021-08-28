using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;

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
        }
    }
}