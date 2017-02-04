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
        private readonly IFactory _factory;
        private readonly ISecurityContext _securityContext;

        public PipelineProcessor(ISecurityContext securityContext, IFactory factory, CommandContext commandContext)
        {
            _securityContext = securityContext;
            _factory = factory;
            Context = commandContext;
        }

        public CommandResult Process<T>(T command)
            where T : IMessage
        {
            var commandProcessor = new CommandProcessor<T>(_factory, Context);
            var validationProcessor = new ValidationProcessor<T>(_factory,commandProcessor);
            var securityProcessor = new SecurityProcessor<T>(_securityContext,validationProcessor);
            return securityProcessor.Process(command);
        }

        public CommandContext Context { get; }
    }
}