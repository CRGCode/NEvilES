using System;
using System.Collections.Generic;
using NEvilES.Abstractions;

namespace NEvilES.Testing
{
    public class TestCommand<TCommand> : ITestCommand where TCommand : ICommand
    {
        public List<IEvent> GivenEvents { get; set; }
        public dynamic Command { get; set; }
        public List<object> Events { get; set; }

        public static TestCommand<TCommand> When(Action<TCommand> func = null)
        {
            var commandBuilder = new TestCommand<TCommand>();
            commandBuilder.Command = Activator.CreateInstance(typeof(TCommand))!; 
            if (func != null)
                func(commandBuilder.Command);

            commandBuilder.GivenEvents = new List<IEvent>();
            commandBuilder.Events = new List<object>();
            return commandBuilder;
        }

        public static TestCommand<TCommand> When(TCommand command)
        {
            var commandBuilder = new TestCommand<TCommand>
            {
                Command = command,
                GivenEvents = new List<IEvent>(),
                Events = new List<object>()
            };
            return commandBuilder;
        }

        public TestCommand<TCommand> Given<TEvent>(Action<TEvent> func = null) where TEvent : class, IEvent, new()
        {
            var evt = new TEvent();
            if (func != null)
                func(evt);
            GivenEvents.Add(evt);

            return this;
        }

        public TestCommand<TCommand> Then<TEvent>(Action<TEvent> func = null) where TEvent : class, IEvent, new()
        {
            var evt = SimpleMapper.Map<TEvent>(Command);
            if (func != null)
                func(evt);
            Events.Add(evt);

            return this;
        }

        public TestCommand<TCommand> Map<TEvent>(Action<TCommand, TEvent> func) where TEvent : class, IEvent, new()
        {
            var evt = SimpleMapper.Map<TEvent>(Command);
            func(Command, evt);
            Events.Add(evt);
            return this;
        }
    }

    public interface ITestCommand
    {
        List<IEvent> GivenEvents { get; }
        dynamic Command { get; }
        List<object> Events { get; }
    }
}