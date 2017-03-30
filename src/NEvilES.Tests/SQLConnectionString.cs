namespace NEvilES.Tests
{
    public class SqlConnectionString : IConnectionString
    {
        public string ConnectionString { get; }

        public SqlConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string DataSource => ConnectionString.Split(';')[0].Split('=')[1];
    }
}