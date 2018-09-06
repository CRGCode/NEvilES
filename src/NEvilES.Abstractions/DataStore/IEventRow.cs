using System;

namespace NEvilES.Abstractions.DataStore
{
    public interface IEventRow
    {
        string Category { get; set; }
        Guid StreamId { get; set; }
        Guid TransactionId { get; set; }
        Guid Who { get; set; }
        string AppVersion { get; set; }
        string BodyType { get; set; }
        string Body { get; set; }
        DateTime When { get; set; }
        int Version { get; set; }
        //Do we really need this?
        // string Metadata { get; set; }
    }
}