using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public class PipelineProcessor : ICommandProcessor
    {
        private readonly ISecurityContext securityContext;
        private readonly IServiceProvider serviceProvider;

        public ICommandContext Context { get; }

        public PipelineProcessor(ISecurityContext securityContext, IServiceProvider serviceProvider, ICommandContext commandContext)
        {
            this.securityContext = securityContext;
            this.serviceProvider = serviceProvider;
            Context = commandContext;
        }

        const int RETRIES = 10;
        private static readonly int[] BackOff = { 10, 20, 50, 100, 200, 300, 500, 600, 700, 1000 };

        public ICommandResult Process<T>(T command) where T : IMessage
        {
            var retry = 0;
            do
            {
                using var factory = new ScopedServiceProviderFactory(serviceProvider.CreateScope());
                var commandProcessor = new CommandProcessor<T>(factory, Context);
                var validationProcessor = new ValidationProcessor<T>(factory, commandProcessor);
                var securityProcessor = new SecurityProcessor<T>(securityContext, validationProcessor);

                try
                {
                    return securityProcessor.Process(command);

                }
                catch (AggregateConcurrencyException)
                {
                    var transaction = (ITransaction)factory.Get(typeof(ITransaction));
                    transaction.Rollback();
                    factory.Dispose();
                    var delay = BackOff[retry++];
                    Thread.Sleep(delay);
                }

            } while (retry < RETRIES);

            throw new PipelineProcessorException(command, retry);
        }
    }

    public class PipelineProcessorException : Exception
    {
        public PipelineProcessorException(IMessage command, int attempts) : 
            base($"Command '{command.GetType().FullName}' failed after {attempts} retries")
        {
        }
    }
}