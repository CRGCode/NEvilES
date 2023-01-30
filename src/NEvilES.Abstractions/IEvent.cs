using System;

namespace NEvilES.Abstractions
{
    public interface IMessage
    {
        Guid GetStreamId();
    }

    public interface ICommand : IMessage
    {
    }

    public interface IPatch :IEvent
    {
        string JSONPath { get; }
        public string Value { get; }
    }

    public class PatchEvent : IPatch
    {
        private readonly Guid streamId;

        public PatchEvent(Guid streamId, string path, string value)
        {
            this.streamId = streamId;
            JSONPath = path;
            Value = value;
        }

        public Guid GetStreamId()
        {
            return streamId;
        }

        public string JSONPath { get; }
        public string Value { get; }
    }


    public interface IEvent : IMessage
    {
    }

    public interface IMapEvent<out TEvent, in TCommand> : IEvent 
        where TEvent : new()
        where TCommand : ICommand
    {
        TEvent Map(TCommand c);
    }
}