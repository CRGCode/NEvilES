using System;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public interface IApprovalWorkflowEngine
    {
        ICommandResult Initiate<TCommand>(TCommand command) where TCommand : ICommand;
        ICommandResult Transition(Guid id, string toState);
    }

    public class ApprovalWorkflowEngine : IApprovalWorkflowEngine
    {
        private readonly PipelineProcessor pipelineProcessor;
        private readonly IRepository repository;

        public ApprovalWorkflowEngine(PipelineProcessor pipelineProcessor, IRepository repository)
        {
            this.pipelineProcessor = pipelineProcessor;
            this.repository = repository;
        }

        public ICommandResult Initiate<TCommand>(TCommand command) where TCommand : ICommand
        {
            return pipelineProcessor.Process(new Approval.Create(CombGuid.NewGuid(), Approval.InnerCommand.Wrap(command)));
        }

        public static dynamic GetCommand(Approval.InnerCommand innerCommand)
        {
            //var obj = JsonConvert.DeserializeObject(innerCommand.Command.ToString(), innerCommand.Type);
            //((IEvent)innerCommand.Command).StreamId = innerCommand.CommandStreamId;
            return innerCommand.Command;
        }

        public static T UnwrapCommand<T>(Approval.InnerCommand innerCommand)
        {
            return (T)innerCommand.Command;
        }
        const string ApprovalEntryPoint = "Approved";
        public ICommandResult Transition(Guid id, string toState)
        {
            //var newState = _secRequestWorkflowProvider.Fire(toState);
            var newState = toState;
            var result = pipelineProcessor.Process(new Approval.ChangeState(id, newState));

            if (newState != ApprovalEntryPoint)
                return result;

            var approvalRequest = repository.Get<Approval.Aggregate>(id);
            var command = GetCommand(approvalRequest.GetInnerCommand());
            return pipelineProcessor.Process(command);
        }
    }
}