using System.Threading.Tasks;

namespace NEvilES.Abstractions.Pipeline
{
    public interface ICommandProcessor
    {
        ICommandResult Process<T>(T command) where T : IMessage;
        Task<ICommandResult> ProcessAsync<T>(T command) where T : IMessage;
    }
    
    public interface IProcessPipelineStage<T> where T : IMessage
    {
        ICommandResult Process(T command);
        Task<ICommandResult> ProcessAsync(T command);
    }
}