using System;
using System.Threading.Tasks;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
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

        public ICommandResult Process(TCommand command)
        {
            if (!securityContext.CheckSecurity())
            {
                throw new Exception("Security Issues.......");
            }
            return innerCommand.Process(command);
        }
    }
}