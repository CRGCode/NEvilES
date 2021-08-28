using System;

namespace NEvilES.DataStore.LiteDb
{
    public class LiteDbEventTable
    {
        public Guid StreamId { get; set; }
        public ulong CommmitedAt { get; set; }
        public string Category { get; set; }
        public Guid TransactionId { get; set; }
        public Guid Who { get; set; }
        public string AppVersion { get; set; }
        public string BodyType { get; set; }
        public string Body { get; set; }
        public DateTimeOffset When { get; set; }
        public int Version { get; set; }
    }
}