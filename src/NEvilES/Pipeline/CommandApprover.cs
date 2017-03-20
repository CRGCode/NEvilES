using System;
using Newtonsoft.Json;

namespace NEvilES.Pipeline
{
    public class CommandApprover : INeedApproval
    {
        private readonly IRepository repository;

        public CommandApprover(IRepository repository)
        {
            this.repository = repository;
        }

        public AggregateCommit Capture<TCommand>(TCommand command) where TCommand : IMessage
        {
            var approvalRequest = new ApprovalRequest.Aggregate();
            approvalRequest.Create(CombGuid.NewGuid(), ApprovalRequest.InnerCommand.Wrap(command));

            return repository.Save(approvalRequest);
        }

        public T UnwrapCommand<T>(ApprovalRequest.InnerCommand innerCommand) where T : IMessage
        {
            var command = (T)JsonConvert.DeserializeObject(innerCommand.Command.ToString(), innerCommand.Type);
            command.StreamId = innerCommand.CommandStreamId;
            return command;
        }

        public dynamic GetCommand(ApprovalRequest.InnerCommand innerCommand)
        {
            var obj = JsonConvert.DeserializeObject(innerCommand.Command.ToString(), innerCommand.Type);
            ((IMessage) obj).StreamId = innerCommand.CommandStreamId;
            return obj;
        }

        public ApprovalResult Approve(Guid id)
        {
            var approvalRequest = repository.Get<ApprovalRequest.Aggregate>(id);
            approvalRequest.Approve();
            var commit = repository.Save(approvalRequest);
            //var method = GetType().GetMethod("UnwrapCommand");
            //var genericMethod = method.MakeGenericMethod(approvalRequest.GetInnerCommand().Type);
            //var command = genericMethod.Invoke(this, new object[] { approvalRequest.GetInnerCommand() });

            return new ApprovalResult(GetCommand(approvalRequest.GetInnerCommand()), commit);
        }
    }

    public abstract class ApprovalRequest
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

        public class Created : IEvent
        {
            public InnerCommand InnerCommand { get; }
            public Guid StreamId { get; set; }

            public Created(Guid streamId, InnerCommand innerCommand)
            {
                StreamId = streamId;
                InnerCommand = innerCommand;
            }
        }

        public class Approve : ICommand
        {
            public Guid StreamId { get; set; }

            public Approve(Guid id)
            {
                StreamId = id;
            }
        }

        public class Approved : IEvent
        {
            public Guid StreamId { get; set; }

            public Approved(Guid streamId)
            {
                StreamId = streamId;
            }
        }

        public class Declined : IEvent
        {
            public Guid StreamId { get; set; }
            public string Reason { get; set; }
        }

        public class Aggregate : AggregateBase,
            IHandleStatelessEvent<Declined>
        {
            public InnerCommand GetInnerCommand()
            {
                return _command;
            }

            public void Create(Guid id, InnerCommand command)
            {
                RaiseEvent(new Created(id, command));
            }

            public void Approve()
            {
                RaiseStateless(new Approved(Id));
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