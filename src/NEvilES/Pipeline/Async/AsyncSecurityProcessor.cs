using System;
using System.Threading.Tasks;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Abstractions.Pipeline.Async;

namespace NEvilES.Pipeline.Async
{
    //public class AsyncSecurityProcessor<TCommand> : IProcessPipelineStageAsync<TCommand>
    //    where TCommand : IMessage
    //{
    //    private readonly ISecurityContext securityContext;
    //    private readonly IProcessPipelineStageAsync<TCommand> innerCommand;

    //    public AsyncSecurityProcessor(ISecurityContext securityContext, IProcessPipelineStageAsync<TCommand> innerCommand)
    //    {
    //        this.securityContext = securityContext;
    //        this.innerCommand = innerCommand;
    //    }

    //    public Task<ICommandResult> ProcessAsync(TCommand command)
    //    {
    //        if (!securityContext.CheckSecurity())
    //        {
    //            throw new Exception("Security Issues.......");
    //        }
    //        return innerCommand.ProcessAsync(command);
    //    }
    //}
}