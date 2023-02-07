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
        string Path { get; }
        public string Value { get; }
    }

    public class PatchEvent : IPatch
    {
        private readonly Guid streamId;

        public PatchEvent(Guid streamId, string path, string value, string tag = null, string operation = null)
        {
            this.streamId = streamId;
            Path = path;
            Value = value;
            Tag = tag;
            Operation = operation;
        }

        public Guid GetStreamId()
        {
            return streamId;
        }

        public string Path { get; }
        public string Value { get; }
        public string Tag { get; }
        public string Operation { get; }
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