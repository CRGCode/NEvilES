using System;
using NEvilES.Abstractions;

namespace NEvilES.Pipeline
{
    public abstract class Approval
    {
        public class InnerCommand
        {
            public Guid CommandStreamId { get; set; }
            public object Command { get; set; }
            public Type Type { get; set; }

            public static InnerCommand Wrap<T>(T command) where T : IMessage
            {
                return new InnerCommand(typeof(T), command, command.StreamId);
            }

            public InnerCommand(Type type, object command, Guid commandStreamId)
            {
                Type = type;
                Command = command;
                CommandStreamId = commandStreamId; // we need to do this because we have explicitly ignored StreamId using [IgnoreDataMember] on IMessage
            }
        }

        public class Create : ICommand
        {
            public InnerCommand InnerCommand { get; set; }
            public Guid StreamId { get; set; }

            public Create(Guid streamId, InnerCommand innerCommand)
            {
                StreamId = streamId;
                InnerCommand = innerCommand;
            }

            public Create(Create c)
            {
                StreamId = c.StreamId;
                InnerCommand = c.InnerCommand;
            }

            protected Create() { }
        }

        public class Created : Create, IEvent
        {
            public Created() { }

            public Created(Create c) : base(c)
            {
            }
        }

        public class ChangeState : ICommand
        {
            public Guid StreamId { get; set; }
            public string NewState { get; }

            public ChangeState(Guid id, string newState)
            {
                StreamId = id;
                NewState = newState;
            }
        }

        public class StateChanged : IEvent
        {
            public Guid StreamId { get; set; }
            public string State { get; set; }

            public StateChanged(Guid streamId, string state)
            {
                StreamId = streamId;
                State = state;
            }
        }

        public class Aggregate : AggregateBase,
            IHandleAggregateCommand<Create>,
            IHandleAggregateCommand<ChangeState>
        {
            public void Handle(Create c)
            {
                RaiseEvent(new Created(c));
            }

            public void Handle(ChangeState c)
            {
                RaiseStateless(new StateChanged(c.StreamId, c.NewState));
            }

            public InnerCommand GetInnerCommand()
            {
                return _command;
            }

            //--------------------------------------------
            private InnerCommand _command;

            private void Apply(Created e)
            {
                Id = e.StreamId;
                _command = e.InnerCommand;
            }
        }
    }
}