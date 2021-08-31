namespace NEvilES.Abstractions.Pipeline
{
    public interface IProcessPipelineStage<T> where T : IMessage
    {
        ICommandResult Process(T command);
    }

    public interface ICommandProcessor
    {
        ICommandResult Process<T>(T command) where T : IMessage;
    }
}