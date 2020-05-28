using System;

namespace NEvilES.Abstractions
{
    public interface IMessage
    {
        Guid StreamId { get; set; }
    }

    public interface ICommand : IMessage { }
    public interface IEvent : IMessage { }
}