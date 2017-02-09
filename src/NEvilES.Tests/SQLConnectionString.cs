namespace NEvilES.Tests
{
    class SQLConnectionString : IConnectionString
    {
        public string ConnectionString { get; }

        public SQLConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}