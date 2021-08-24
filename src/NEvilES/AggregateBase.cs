using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NEvilES.Abstractions;

namespace NEvilES
{
    public interface IAggregateHandlers
    {
        IDictionary<Type, MethodInfo> Handlers { get; set; }
    }

    public class AggregateMethodCache
    {
        public IDictionary<Type, Action<IAggregate, object>> ApplyMethods { get; set; }
        public IDictionary<Type, MethodInfo> HandlerMethods { get; set; }
    }

    public abstract class AggregateBase : IAggregate, IEquatable<IAggregate>, IAggregateHandlers
    {
        private static readonly IDictionary<Type, AggregateMethodCache> AggregateTypes =
            new Dictionary<Type, AggregateMethodCache>();

        private readonly IDictionary<Type, Action<IAggregate, object>> applyMethods;
        IDictionary<Type, MethodInfo> IAggregateHandlers.Handlers { get; set; }
        private readonly ICollection<IEventData> uncommittedEvents = new LinkedList<IEventData>();

        protected AggregateBase()
        {
            var type = GetType();
            if (!AggregateTypes.ContainsKey(type))
            {
                lock (AggregateTypes)
                {
                    if (!AggregateTypes.ContainsKey(type))
                    {
                        Register(GetType());
                    }
                }
            }

            var methodCache = AggregateTypes[type];

            applyMethods = methodCache.ApplyMethods;
            ((IAggregateHandlers) this).Handlers = methodCache.HandlerMethods;
        }

        public Guid Id { get; protected set; }
        public int Version { get; private set; }

        void IAggregate.ApplyEvent<T>(T @event)
        {
            var type = @event.GetType();
            var foundHandlers = FindApplyHandler(type);

            foreach (var handler in foundHandlers)
            {
                handler.Invoke(this, @event);
            }

            Version++;
        }

        private IEnumerable<Action<IAggregate, object>> FindApplyHandler(Type type)
        {
            if (applyMethods.ContainsKey(type))
            {
                yield return applyMethods[type];
            }
            else
            {
                var interfaces = type.GetTypeInfo().GetInterfaces();
                foreach (var i in interfaces)
                {
                    if (applyMethods.ContainsKey(i))
                    {
                        yield return applyMethods[i];
                    }
                }
            }
        }

        ICollection IAggregate.GetUncommittedEvents()
        {
            return (ICollection) uncommittedEvents;
        }

        void IAggregate.ClearUncommittedEvents()
        {
            uncommittedEvents.Clear();
        }

        public virtual bool Equals(IAggregate other)
        {
            return other != null && other.Id == Id;
        }

        private static void Register(Type aggregateType)
        {
            if (aggregateType == null)
                throw new ArgumentNullException(nameof(aggregateType));

            if (AggregateTypes.TryGetValue(aggregateType, out _))
                return;

            var applyMethods = new Dictionary<Type, Action<IAggregate, object>>();
            foreach (var apply in GetMethods(aggregateType,"Apply"))
            {
                var applyMethod = apply.MethodInfo;

                if (applyMethods.ContainsKey(apply.Type))
                {
                    throw new Exception($"'{apply.Type.FullName}' has previously been registered. Please check for duplicate Apply methods for this type.");
                }

                applyMethods.Add(apply.Type, (a, m) => applyMethod.Invoke(a, new[] {m}));
            }

            var handlerMethods = GetMethods(aggregateType,"Handle").ToDictionary(x => x.Type, x => x.MethodInfo);

            AggregateTypes[aggregateType] = new AggregateMethodCache
            {
                ApplyMethods = applyMethods,
                HandlerMethods = handlerMethods
            };
        }

        private static IEnumerable<TypeToMethod> GetMethods(Type type, string name)
        {
            do
            {
                var methods = type.GetTypeInfo().DeclaredMethods;
                foreach (var m in methods
                    .Where(m => m.Name == name && m.GetParameters().Length >= 1 && m.ReturnType == typeof(void)))
                {
                    yield return new TypeToMethod(m.GetParameters().First().ParameterType, m);
                }

                type = type.GetTypeInfo().BaseType;
            } while (type != null);
        }

        private class TypeToMethod
        {
            public TypeToMethod(Type type, MethodInfo m)
            {
                Type = type;
                MethodInfo = m;
            }

            public Type Type { get; }
            public MethodInfo MethodInfo { get; }

            public override string ToString()
            {
                return MethodInfo.ToString();
            }
        }

        public void Raise<TEvent>(object command) where TEvent : class, IEvent, new()
        {
            var evt = SimpleMapper.Map<TEvent>(command);

            RaiseEvent(evt);
        }

        public virtual void RaiseEvent<T>(T evt) where T : IEvent
        {
            var type = typeof(T);
            if (!FindApplyHandler(type).Any())
            {
                throw new Exception($"You have forgotten to add private event method for '{type}' to aggregate '{GetType()}'");
            }
            ((IAggregate) this).ApplyEvent(evt);
            uncommittedEvents.Add(new EventData(type, evt, DateTime.UtcNow, Version));
        }

        public void RaiseStateless<TEvent>(object command) where TEvent : class, IEvent, new()
        {
            var evt = SimpleMapper.Map<TEvent>(command);

            RaiseStatelessEvent(evt);
        }

        public void RaiseStatelessEvent<T>(T msg) where T : IEvent
        {
            var type = msg.GetType();
            if (FindApplyHandler(type).Any())
            {
                throw new Exception($"You can't RaiseStatelessEvent - There's a 'private void Apply({type} e)' method on this '{GetType()}' aggregate!");
            }
            Version++;
            uncommittedEvents.Add(new EventData(type, msg, DateTime.UtcNow, Version));
        }

        public override int GetHashCode()
        {
            // TODO Id should be readonly property as change this changes the Hashcode!
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IAggregate);
        }

        public void GuardFromEmptyOrNull(string name, object o)
        {
            if (o == null)
            {
                throw new DomainAggregateException(this, "{0} can't have null values", name);
            }

            if (o is string s && string.IsNullOrWhiteSpace(s))
            {
                throw new DomainAggregateException(this, "{0} can't be empty or have null values", name);
            }
        }

        public void GuardFromEmptyOrNulls(params object[] args)
        {
            foreach (var o in args)
            {
                GuardFromEmptyOrNull(GetType().Name, o);
            }
        }

        public void SetState(Guid id, int version = 0)
        {
            Id = id;
            if (version != 0)
            {
                Version = version;
            }
        }
    }

    public static class SimpleMapper
    {
        public static TEvent Map<TEvent>(object command) where TEvent : class, new()
        {
            var evt = new TEvent();

            var cmdProps = command.GetType().GetTypeInfo().GetProperties().Where(x => x.CanRead).ToList();
            var evtProps = typeof(TEvent).GetTypeInfo().GetProperties()
                    .Where(x => x.CanWrite)
                    .ToList();

            foreach (var sourceProp in cmdProps)
            {
                if (evtProps.All(x => x.Name != sourceProp.Name))
                    continue;
                var p = evtProps.First(x => x.Name == sourceProp.Name);
                p.SetValue(evt, sourceProp.GetValue(command, null), null);
            }

            return evt;
        }
    }

    public class DomainAggregateException : Exception
    {
        public readonly AggregateBase Aggregate;

        public DomainAggregateException(AggregateBase aggregate, string message, params object[] args)
            : base(string.Format(message, args))
        {
            Aggregate = aggregate;
        }
    }
}