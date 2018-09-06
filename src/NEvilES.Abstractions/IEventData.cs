using System;

namespace NEvilES.Abstractions
{
    public interface IEventData
    {
        Type Type { get; }
        DateTime TimeStamp { get; }
        int Version { get; }
        object Event { get; }
    }
}