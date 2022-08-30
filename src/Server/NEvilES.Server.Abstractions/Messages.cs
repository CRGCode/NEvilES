using System;
using System.Collections.Generic;

namespace NEvilES.Server.Abstractions
{
    public class StoreEventsRequest
    {
        public Guid StreamId { get; set; }
        public int CurrentVersion { get; set; }
        public object Username { get; set; }
        public string Source { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public List<Tuple<string, object>> Events { get; set; }
    }

    public class LoadEventsReply
    {
        public LoadEventsReply()
        {
            Events = new List<EventMessage>();
        }
        public List<EventMessage> Events { get; set; }
    }

    public class CatchUpRequest
    {
        public int From { get; set; }
        public int To { get; set; }
    }

    public class PublishEvents
    {
        public PublishEvents(int eventCount)
        {
            Events = new List<EventMessage>();
            LastEventProcessed = eventCount;
        }
        public int LastEventProcessed { get; set; }
        public List<EventMessage> Events { get; set; }
    }

    public class EventMessage
    {
        public string Source { get; set; }
        public Guid StreamId { get; private set; }
        public int Version { get; private set; }
        public string Type { get; private set; }
        public byte[] Data { get; private set; }

        //public EventMessage(string type, byte[] data)
        //{
        //	Data = data;
        //	Type = type;
        //}
        public EventMessage(string source, Guid streamId,int version, string type, byte[] data)
        {
            Source = source;
            StreamId = streamId;
            Version = version;
            Data = data;
            Type = type;
        }
    }
 }
