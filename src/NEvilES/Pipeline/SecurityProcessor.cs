﻿using System;
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

     public class SecurityProcessor<TCommand> : IProcessPipelineStage<TCommand>
        where TCommand : IMessage
    {
        private readonly ISecurityContext securityContext;
        private readonly IProcessPipelineStage<TCommand> innerCommand;
        private readonly ILogger logger;

        public SecurityProcessor(ISecurityContext securityContext, IProcessPipelineStage<TCommand> innerCommand, ILogger logger)
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
    }
}