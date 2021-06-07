﻿using NEvilES.Abstractions;
using System;

namespace NEvilES
{
    public class Event : IEvent
    {
        public Guid StreamId { get; set; }
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
}