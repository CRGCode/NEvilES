using System;
using System.Collections;

namespace NEvilES.Abstractions
{
    public interface IAggregate
    {
        Guid Id { get; }
        int Version { get; }

        void ApplyEvent<T>(T @event) where T : IMessage;
        ICollection GetUncommittedEvents();
        void ClearUncommittedEvents();

        void Raise<TEvent>(object command) where TEvent : class, IEvent, new();
        void RaiseEvent<T>(T evt) where T : IEvent;
        void RaiseStateless<T>(T msg) where T : IEvent;
    }

    public interface IStatelessAggregate
    {
        void RaiseStatelessEvent<T>(T msg) where T : IMessage;
    }


}