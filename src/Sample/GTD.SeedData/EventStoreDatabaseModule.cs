using System.Data;
using System.Data.SqlClient;
using Autofac;
using GTD.Common;

namespace GTD.SeedData
{
    public class EventStoreDatabaseModule : Module
    {
        public string ConnectionString { get; set; }

        public EventStoreDatabaseModule(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new SqlConnectionString(ConnectionString))
                .As<IConnectionString>().SingleInstance();

            builder.Register(c =>
            {
                var conn = new SqlConnection(c.Resolve<IConnectionString>().ConnectionString);
                conn.Open();
                return conn;
            }).As<IDbConnection>().InstancePerLifetimeScope();
            builder.Register(c =>
            {
                var conn = c.Resolve<IDbConnection>();
                return conn.BeginTransaction();
            }).As<IDbTransaction>().InstancePerLifetimeScope();
        }
    }
}