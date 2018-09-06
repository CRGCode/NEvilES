using System;

namespace NEvilES.Abstractions
{
    public interface IEventTypeLookupStrategy
    {
        IEventTypeLookupStrategy ScanAssemblyOfType(Type type);
        Type Resolve(string name);
    }
}