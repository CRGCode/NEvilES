using System;
using System.Collections;

namespace NEvilES.Abstractions
{
    public interface IAggregate
    {
        Guid Id { get; }
        int Version { get; }

        T ApplyEvent<T>(T @event) where T : IMessage;
        ICollection GetUncommittedEvents();
        void ClearUncommittedEvents();

        TEvent Raise<TEvent>(object command) where TEvent : IEvent, new();
        TEvent RaiseEvent<TEvent>(TEvent evt) where TEvent : IEvent;
        TEvent RaiseStateless<TEvent>(object command) where TEvent : IEvent, new();
        TEvent RaiseStatelessEvent<TEvent>(TEvent evt) where TEvent : IEvent;
    }

    public interface IStatelessAggregate
    {
        T RaiseStatelessEvent<T>(T msg) where T : IMessage;
    }


}