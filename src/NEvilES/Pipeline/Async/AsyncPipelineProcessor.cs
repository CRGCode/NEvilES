using System.Threading.Tasks;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Abstractions.Pipeline.Async;
using NEvilES.Pipeline.Async;

namespace NEvilES.Pipeline
{
    //public class AsyncPipelineProcessor : IAsyncCommandProcessor
    //{
    //    private readonly IFactory factory;
    //    private readonly ISecurityContext securityContext;

    //    public AsyncPipelineProcessor(ISecurityContext securityContext, IFactory factory)
    //    {
    //        this.securityContext = securityContext;
    //        this.factory = factory;
    //    }

    //    public Task<ICommandResult> ProcessAsync<T>(T command)
    //        where T : IMessage
    //    {
    //        var context = (ICommandContext)factory.Get(typeof(ICommandContext));
    //        var commandProcessor = new AsyncCommandProcessor<T>(factory, context);
    //        var validationProcessor = new AsyncValidationProcessor<T>(factory, commandProcessor);
    //        var securityProcessor = new AsyncSecurityProcessor<T>(securityContext, validationProcessor);
    //        return securityProcessor.ProcessAsync(command);
    //    }
    //}
}