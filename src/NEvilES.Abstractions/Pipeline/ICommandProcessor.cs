using System.Threading.Tasks;

namespace NEvilES.Abstractions.Pipeline
{
    public interface ICommandProcessor
    {
        ICommandResult Process<T>(T command) where T : IMessage;
        Task<ICommandResult> ProcessAsync<T>(T command) where T : IMessage;
    }
}