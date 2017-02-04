using System;
using System.Collections.Generic;
using System.Linq;

namespace NEvilES.DataStore
{
    public interface IEventTypeLookupStrategy
    {
        IEventTypeLookupStrategy ScanAssemblyOfType(Type type);
        Type Resolve(string name);
    }

    public class EventTypeLookupStrategy : IEventTypeLookupStrategy
    {
        private static readonly Type _eventType = typeof(IMessage);
        private static readonly Type _aggType = typeof(IAggregate);
        private readonly Dictionary<string, Type> _nameToType = new Dictionary<string, Type>();

        public IEventTypeLookupStrategy ScanAssemblyOfType(Type type)
        {
            var candidateTypes = type.Assembly.GetTypes().Where(x =>
                x.IsClass && !x.IsAbstract && (x.GetInterfaces().Contains(_eventType) || x.GetInterfaces().Contains(_aggType))).ToArray();

            foreach (var candidateType in candidateTypes)
            {
                var name = candidateType.FullName;

                if (!_nameToType.ContainsKey(name))
                {
                    _nameToType.Add(name, candidateType);
                }
            }

            return this;
        }

        public Type Resolve(string name)
        {
            if (!_nameToType.ContainsKey(name))
            {
                throw new CouldNotResolveEventTypeException(name);
            }

            return _nameToType[name];
        }
    }

    public class CouldNotResolveEventTypeException : Exception
    {
        public CouldNotResolveEventTypeException(string typeName)
            : base(string.Format("Could not resolve event named '{0}' to a reflected type", typeName))
        { }
    }
}