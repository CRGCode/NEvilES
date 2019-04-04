using NEvilES;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;

namespace NEvilES.DataStore.DynamoDB
{
    public class DynamoDBTransaction : TransactionBase
    {
        public DynamoDBTransaction()
        {
            Id = CombGuid.NewGuid();
        }
    }

}