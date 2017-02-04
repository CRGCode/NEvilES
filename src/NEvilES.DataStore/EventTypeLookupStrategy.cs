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
        private static readonly Type EventType = typeof(IMessage);
        private static readonly Type AggType = typeof(IAggregate);
        private readonly Dictionary<string, Type> nameToType = new Dictionary<string, Type>();

        public IEventTypeLookupStrategy ScanAssemblyOfType(Type type)
        {
            var candidateTypes = type.Assembly.GetTypes().Where(x =>
                x.IsClass && !x.IsAbstract && (x.GetInterfaces().Contains(EventType) || x.GetInterfaces().Contains(AggType))).ToArray();

            foreach (var candidateType in candidateTypes)
            {
                var name = candidateType.FullName;

                if (!nameToType.ContainsKey(name))
                {
                    nameToType.Add(name, candidateType);
                }
            }

            return this;
        }

        public Type Resolve(string name)
        {
            if (!nameToType.ContainsKey(name))
            {
                throw new CouldNotResolveEventTypeException(name);
            }

            return nameToType[name];
        }
    }

    public class CouldNotResolveEventTypeException : Exception
    {
        public CouldNotResolveEventTypeException(string typeName)
            : base(string.Format("Could not resolve event named '{0}' to a reflected type", typeName))
        { }
    }
}