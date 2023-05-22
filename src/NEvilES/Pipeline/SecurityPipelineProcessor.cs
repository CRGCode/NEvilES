using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

     public class SecurityPipelineProcessor : PipelineStage
    {
        private readonly ISecurityContext securityContext;

        public SecurityPipelineProcessor(IFactory factory, IProcessPipelineStage nextPipelineStage, ILogger logger)
            : base(factory, nextPipelineStage, logger)
        {
            securityContext = (ISecurityContext)factory.Get(typeof(ISecurityContext));
        }


        public override ICommandResult Process<TCommand>(TCommand command)
        {
            if (!securityContext.CheckSecurity())
            {
                Logger.LogWarning("Security Issues");
                throw new Exception("Security Issues.......");
            }
            Logger.LogTrace("Security Checked");

            return NextPipelineStage.Process(command);
        }

        public override Task<ICommandResult> ProcessAsync<TCommand>(TCommand command)
        {
            if (!securityContext.CheckSecurity())
            {
                throw new Exception("Security Issues.......");
            }
            Logger.LogTrace("Security Checked");

            return NextPipelineStage.ProcessAsync(command);
        }
    }
}