using System;
using NEvilES.Abstractions;

namespace NEvilES.Pipeline
{
    public abstract class Approval
    {
        public abstract class Id : IMessage
        {
            public Guid GetStreamId() => ApprovalId;
            public Guid ApprovalId { get; set; }
        }

        public class InnerCommand
        {
            public Guid CommandStreamId { get; set; }
            public object Command { get; set; }
            public Type Type { get; set; }

            public static InnerCommand Wrap<T>(T command) where T : ICommand
            {
                return new InnerCommand(typeof(T), command, command.GetStreamId());
            }

            public InnerCommand(Type type, object command, Guid commandStreamId)
            {
                Type = type;
                Command = command;
                CommandStreamId = commandStreamId; // we need to do this because we have explicitly ignored StreamId using [IgnoreDataMember] on IMessage
            }
        }

        public class Create : Id, ICommand
        {
            public InnerCommand InnerCommand { get; set; }

            public Create(Guid approvalId, InnerCommand innerCommand)
            {
                ApprovalId = approvalId;
                InnerCommand = innerCommand;
            }

            public Create(Create c)
            {
                ApprovalId = c.ApprovalId;
                InnerCommand = c.InnerCommand;
            }

            protected Create() { }
            //{
            //    throw new NotImplementedException();
            //}
        }

        public class Created : Create, IEvent
        {
            public Created() { }

            public Created(Create c) : base(c)
            {
                ApprovalId = c.GetStreamId();
            }
        }

        public class ChangeState : Id, ICommand
        {
            public string NewState { get; }

            public ChangeState(Guid approvalId, string newState)
            {
                ApprovalId = approvalId;
                NewState = newState;
            }
        }

        public class StateChanged : Id, IEvent
        {
            public string State { get; set; }

            public StateChanged(Guid approvalId, string state)
            {
                ApprovalId = approvalId;
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
                RaiseStatelessEvent(new StateChanged(c.GetStreamId(), c.NewState));
            }

            public InnerCommand GetInnerCommand()
            {
                return command;
            }

            //--------------------------------------------
            private InnerCommand command;

            private void Apply(Created e)
            {
                Id = e.ApprovalId;
                command = e.InnerCommand;
            }
        }
    }
}