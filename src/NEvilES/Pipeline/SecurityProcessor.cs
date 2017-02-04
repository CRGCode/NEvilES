using System;

namespace NEvilES.Pipeline
{
    public interface ISecurityContext
    {
        bool CheckSecurity();
    }

    public class SecurityContext : ISecurityContext
    {
        public bool CheckSecurity()
        {
            return true;
        }
    }

    public class SecurityProcessor<TCommand> : IProcessPipelineStage<TCommand>
        where TCommand : IMessage
    {
        private readonly ISecurityContext securityContext;
        private readonly IProcessPipelineStage<TCommand> innerCommand;

        public SecurityProcessor(ISecurityContext securityContext, IProcessPipelineStage<TCommand> innerCommand)
        {
            this.securityContext = securityContext;
            this.innerCommand = innerCommand;
        }

        public CommandResult Process(TCommand command)
        {
            if (!securityContext.CheckSecurity())
            {
                throw new Exception("Security Issues.......");
            }
            return innerCommand.Process(command);
        }
    }
}