using System.Threading.Tasks;

namespace NEvilES.Abstractions.Pipeline.Async
{
    public interface IProcessPipelineStageAsync<T> where T : IMessage
    {
        Task<ICommandResult> ProcessAsync(T command);
    }

    public interface IAsyncCommandProcessor
    {
        Task<ICommandResult> ProcessAsync<T>(T command) where T : IMessage;
        ICommandContext Context { get; }
    }
}