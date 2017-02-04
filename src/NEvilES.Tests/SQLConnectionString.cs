namespace NEvilES.Tests
{
    class SqlConnectionString : IConnectionString
    {
        public string ConnectionString { get; }

        public SqlConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}