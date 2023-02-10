using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

     public class SecurityPipelineProcessor<TCommand> : IProcessPipelineStage<TCommand>
        where TCommand : IMessage
    {
        private readonly ISecurityContext securityContext;
        private readonly IProcessPipelineStage<TCommand> innerCommand;
        private readonly ILogger logger;

        public SecurityPipelineProcessor(ISecurityContext securityContext, IProcessPipelineStage<TCommand> innerCommand, ILogger logger)
        {
            this.securityContext = securityContext;
            this.innerCommand = innerCommand;
            this.logger = logger;
        }

        public ICommandResult Process(TCommand command)
        {
            if (!securityContext.CheckSecurity())
            {
                logger.LogWarning("Security Issues");
                throw new Exception("Security Issues.......");
            }
            logger.LogTrace("Security Checked");

            return innerCommand.Process(command);
        }

        public Task<ICommandResult> ProcessAsync(TCommand command)
        {
            if (!securityContext.CheckSecurity())
            {
                throw new Exception("Security Issues.......");
            }
            logger.LogTrace("Security Checked");

            return innerCommand.ProcessAsync(command);
        }
    }
}