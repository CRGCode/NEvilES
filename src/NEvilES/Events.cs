using Newtonsoft.Json;
using System;
using System.Collections;

namespace NEvilES
{
    public interface IMessage
    {
        [JsonIgnore]
        Guid StreamId { get; set; }
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

        void RaiseEvent<T>(T msg) where T : IEvent;
        void RaiseStatelessEvent<T>(T msg) where T : IEvent;
    }

    public interface IStatelessAggregate
    {
        void RaiseStatelessEvent<T>(T msg) where T : IMessage;
    }

    public interface IEventData
    {
        string Type { get; }
        DateTime TimeStamp { get; }
        int Version { get; }
        object Event { get; }
    }

    public class EventData : IEventData
    {
        public string Type { get; }
        public object Event { get; }
        public DateTime TimeStamp { get; }
        public int Version { get; }

        public EventData(string type, object @event, DateTime stamp, int version)
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
    }
}