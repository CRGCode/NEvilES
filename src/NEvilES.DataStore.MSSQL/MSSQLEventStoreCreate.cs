using System.Data.SqlClient;
using NEvilES.Abstractions;
using NEvilES.Abstractions.DataStore;

namespace NEvilES.DataStore.MSSQL
{
    public class MSSQLEventStoreCreate : ICreateOrWipeDb
    {
        private readonly string dbName;
        private readonly string targetDb;
        private readonly string master;

        public MSSQLEventStoreCreate(IConnectionString connString)
        {
            dbName = connString.Keys["Database"];

            targetDb = connString.Data;

            master = $"Server={connString.Keys["Server"]};Database=Master;Integrated Security=true;";
        }

        public void CreateOrWipeDb()
        {
            using (var connection = new SqlConnection(master))
            {
                connection.Open();

                var createDb = string.Format(@"
IF EXISTS(SELECT * FROM sys.databases WHERE name='{0}')
BEGIN
	ALTER DATABASE [{0}]
	SET SINGLE_USER
	WITH ROLLBACK IMMEDIATE
	DROP DATABASE [{0}]
END

DECLARE @FILENAME AS VARCHAR(255)

SET @FILENAME = CONVERT(VARCHAR(255), SERVERPROPERTY('instancedefaultdatapath')) + '{0}';

EXEC ('CREATE DATABASE [{0}] ON PRIMARY
	(NAME = [{0}],
	FILENAME =''' + @FILENAME + ''',
	SIZE = 250MB,
	MAXSIZE = 5000MB,
	FILEGROWTH = 5MB )')
", dbName);
                var command = connection.CreateCommand();
                command.CommandText = createDb;
                command.ExecuteNonQuery();
                connection.Close();
            }

            using (var connection = new SqlConnection(targetDb))
            {
                connection.Open();
                var cmd = connection.CreateCommand();

                cmd.CommandText = @"
CREATE TABLE events(
        [id] [bigint] IDENTITY(1,1) NOT NULL,
        [category] [nvarchar](500) NOT NULL,
        [streamid] [uniqueidentifier] NOT NULL,
        [transactionid] [uniqueidentifier] NOT NULL,
        [bodytype] [nvarchar](500) NOT NULL,
        [body] [nvarchar](max) NOT NULL,
        [who] [uniqueidentifier] NOT NULL,
        [_when] [datetime] NOT NULL,
        [version] [int] NOT NULL,
        [appversion] [nvarchar](20) NOT NULL,
    PRIMARY KEY CLUSTERED
    (
	       [id] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT UQ_StreamVersion UNIQUE(streamid,[version]) 
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
";
                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }
    }
}