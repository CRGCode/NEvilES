using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.DynamoDB
{
    public class DynamoDBTransaction : TransactionBase
    {
        public DynamoDBTransaction()
        {
            Id = CombGuid.NewGuid();
        }
    }


    public class TableConstants
    {
        public static string EVENT_TABLE_NAME = "eventstore";
    }

}