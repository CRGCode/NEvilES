using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public class PipelineProcessor : ICommandProcessor
    {
        private readonly IFactory factory;
        private readonly ISecurityContext securityContext;

        public PipelineProcessor(ISecurityContext securityContext, IFactory factory, ICommandContext commandContext)
        {
            this.securityContext = securityContext;
            this.factory = factory;
            Context = commandContext;
        }

        public ICommandResult Process<T>(T command)
            where T : IMessage
        {
            var commandProcessor = new CommandProcessor<T>(factory, Context);
            var validationProcessor = new ValidationProcessor<T>(factory, commandProcessor);
            var securityProcessor = new SecurityProcessor<T>(securityContext, validationProcessor);
            return securityProcessor.Process(command);
        }

        public ICommandContext Context { get; }
    }
}