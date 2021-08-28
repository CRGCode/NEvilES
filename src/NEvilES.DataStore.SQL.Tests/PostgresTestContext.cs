using System.Data;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using Npgsql;

namespace NEvilES.DataStore.SQL.Tests
{
    public class PostgresTestContext : BaseTestContext
    {
        public PostgresTestContext() : base("Host=localhost;Username=postgres;Password=password;Database=originations")
        {
        }

        protected override void AddServices(IServiceCollection services)
        {
            services.AddScoped<IDbConnection>(c =>
            {
                var conn = new NpgsqlConnection(c.GetRequiredService<IConnectionString>().Data);
                conn.Open();
                return conn;
            });
        }
    }
}
