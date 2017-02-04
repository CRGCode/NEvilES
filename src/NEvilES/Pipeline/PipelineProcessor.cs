namespace NEvilES.Pipeline
{
    public interface IProcessPipelineStage<T> where T : IMessage
    {
        CommandResult Process(T command);
    }

    public interface ICommandProcessor
    {
        CommandResult Process<T>(T command) where T : IMessage;
        CommandContext Context { get; }
    }

    public class PipelineProcessor : ICommandProcessor
    {
        private readonly IFactory factory;
        private readonly ISecurityContext securityContext;

        public PipelineProcessor(ISecurityContext securityContext, IFactory factory, CommandContext commandContext)
        {
            this.securityContext = securityContext;
            this.factory = factory;
            Context = commandContext;
        }

        public CommandResult Process<T>(T command)
            where T : IMessage
        {
            var commandProcessor = new CommandProcessor<T>(factory, Context);
            var validationProcessor = new ValidationProcessor<T>(factory,commandProcessor);
            var securityProcessor = new SecurityProcessor<T>(securityContext,validationProcessor);
            return securityProcessor.Process(command);
        }

        public CommandContext Context { get; }
    }
}