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