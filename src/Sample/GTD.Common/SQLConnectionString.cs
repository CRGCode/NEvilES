namespace GTD.Common
{
    public interface IConnectionString
    {
        string ConnectionString { get; }
    }

    public class SqlConnectionString : IConnectionString
    {
        public string ConnectionString { get; }

        public SqlConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}