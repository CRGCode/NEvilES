using Newtonsoft.Json;
using System;
using System.Collections;
using System.Linq;

namespace NEvilES
{
    public interface IMessage
    {
        [JsonIgnore]
        Guid StreamId { get; set; }
    }

    public class Event : IEvent
    {
        //public static Type GetRealType<T>(T @event)
        //{
        //    var type = typeof(T);
        //    if (type == typeof(IEvent))
        //        return @event.GetType();
        //    else
        //        return type;
        //}

        public Guid StreamId { get; set; }
    }

    public interface ICommand : IMessage { }
    public interface IEvent : IMessage { }

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

    public interface IEventData
    {
        Type Type { get; }
        DateTime TimeStamp { get; }
        int Version { get; }
        object Event { get; }
    }

    public class EventData : IEventData
    {
        public Type Type { get; }
        public object Event { get; }
        public DateTime TimeStamp { get; }
        public int Version { get; }

        public EventData(Type type, object @event, DateTime stamp, int version)
        {
            Event = @event;
            Type = type;
            TimeStamp = stamp;
            Version = version;
        }
    }

    public interface IRepository
    {
        TAggregate Get<TAggregate>(Guid id) where TAggregate : IAggregate;
        IAggregate Get(Type type, Guid id);
        IAggregate GetStateless(Type type, Guid id);
        AggregateCommit Save(IAggregate aggregate);
    }

    public class AggregateCommit
    {
        public Guid StreamId { get; set; }
        public Guid By { get; set; }
        public string Metadata { get; set; }
        public IEventData[] UpdatedEvents { get; set; }

        public AggregateCommit(Guid streamId, Guid by, string metadata, IEventData[] updatedEvents)
        {
            StreamId = streamId;
            By = by;
            Metadata = metadata;
            UpdatedEvents = updatedEvents;
        }

        public override string ToString()
        {
            return string.Join(",", UpdatedEvents.Select(x => x.Event));
        }
    }
}