using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace NEvilES.Abstractions.Pipeline
{
    public interface IProcessPipelineStage
    {
        ICommandResult Process<T>(T command) where T : IMessage;
        Task<ICommandResult> ProcessAsync<T>(T command) where T : IMessage;
    }

    public abstract class PipelineStage: IProcessPipelineStage
    {
        protected readonly IFactory Factory;
        protected readonly IProcessPipelineStage NextPipelineStage;
        protected readonly ILogger Logger;
        protected PipelineStage(IFactory factory, IProcessPipelineStage nextPipelineStage, ILogger logger)
        {
            Factory = factory;
            NextPipelineStage = nextPipelineStage;
            Logger = logger;
        }

        public abstract ICommandResult Process<T>(T command) where T : IMessage;
        public abstract Task<ICommandResult> ProcessAsync<T>(T command) where T : IMessage;
    }
}