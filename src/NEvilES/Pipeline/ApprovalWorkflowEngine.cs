using System;
using Newtonsoft.Json;

namespace NEvilES.Pipeline

{
    public interface IApprovalWorkflowEngine
    {
        CommandResult Initiate<TCommand>(TCommand command) where TCommand : IMessage;
        CommandResult Transition(Guid id, string toState);
    }

    public class ApprovalWorkflowEngine : IApprovalWorkflowEngine
    {
        private readonly ICommandProcessor _commandProcessor;
        private readonly IRepository _repository;

        public ApprovalWorkflowEngine(ICommandProcessor commandProcessor, IRepository repository)
        {
            _commandProcessor = commandProcessor;
            _repository = repository;
        }

        public CommandResult Initiate<TCommand>(TCommand command) where TCommand : IMessage
        {
            return _commandProcessor.Process(new Approval.Create(CombGuid.NewGuid(), Approval.InnerCommand.Wrap(command)));
        }

        public static dynamic GetCommand(Approval.InnerCommand innerCommand)
        {
            //var obj = JsonConvert.DeserializeObject(innerCommand.Command.ToString(), innerCommand.Type);
            ((IMessage)innerCommand.Command).StreamId = innerCommand.CommandStreamId;
            return innerCommand.Command;
        }

        const string ApprovalEntryPoint = "Approved";
        public CommandResult Transition(Guid id, string toState)
        {
            //var newState = _secRequestWorkflowProvider.Fire(toState);
            var newState = toState;
            var result = _commandProcessor.Process(new Approval.ChangeState(id, newState));

            if (newState != ApprovalEntryPoint)
                return result;

            var approvalRequest = _repository.Get<Approval.Aggregate>(id);
            var command = GetCommand(approvalRequest.GetInnerCommand());
            return _commandProcessor.Process(command);
        }
    }
}