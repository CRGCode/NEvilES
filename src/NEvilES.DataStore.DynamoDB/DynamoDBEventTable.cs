using System;
using Amazon.DynamoDBv2.DataModel;
using NEvilES.DataStore.DynamoDB.Converters;

namespace NEvilES.DataStore.DynamoDB
{
    [DynamoDBTable("eventstore")]
    public class DynamoDBEventTable
    {

        [DynamoDBHashKey]
        public Guid StreamId { get; set; }
        public ulong CommmitedAt { get; set; }
        public string Category { get; set; }
        public Guid TransactionId { get; set; }
        public Guid Who { get; set; }
        public string AppVersion { get; set; }
        public string BodyType { get; set; }
        public string Body { get; set; }

        [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset When { get; set; }

        [DynamoDBRangeKey]
        public int Version { get; set; }

        //Do we really need this?
        // public string Metadata { get; set; }
    }
}