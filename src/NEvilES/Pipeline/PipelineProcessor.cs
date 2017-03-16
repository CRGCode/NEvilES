using System;
using System.Linq;

namespace NEvilES.Pipeline
{
    public interface IProcessPipelineStage<T> where T : IMessage
    {
        CommandResult Process(T command);
    }

    public class ApprovalResult
    {
        public AggregateCommit Commit { get; }
        public object Command { get; }
        public ApprovalResult(object command, AggregateCommit commit)
        {
            Command = command;
            Commit = commit;
        }
    }

    public interface INeedApproval
    {
        AggregateCommit Capture<TCommand>(TCommand command) where TCommand : IMessage;
        ApprovalResult Approve(Guid id);
    }

    public interface ICommandProcessor
    {
        CommandResult Process<T>(T command) where T : IMessage;
        CommandContext Context { get; }
    }

    public class PipelineProcessor : ICommandProcessor
    {
        private readonly IFactory factory;
        private readonly INeedApproval approvalStep;
        private readonly ISecurityContext securityContext;

        public PipelineProcessor(ISecurityContext securityContext, IFactory factory, CommandContext commandContext, INeedApproval approvalStep = null)
        {
            this.securityContext = securityContext;
            this.factory = factory;
            this.approvalStep = approvalStep;
            Context = commandContext;
        }

        public CommandResult Process<T>(T command)
            where T : IMessage
        {
            var commandProcessor = new CommandProcessor<T>(factory, Context, approvalStep);
            var validationProcessor = new ValidationProcessor<T>(factory,commandProcessor);
            var securityProcessor = new SecurityProcessor<T>(securityContext,validationProcessor);
            return securityProcessor.Process(command);
        }

        public CommandContext Context { get; }
    }
}