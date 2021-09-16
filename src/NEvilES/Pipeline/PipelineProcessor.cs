using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public class PipelineProcessor : ICommandProcessor
    {
        private readonly IFactory factory;

        public PipelineProcessor(IFactory factory)
        {
            this.factory = factory;
        }

        const int RETRIES = 10;
        private static readonly int[] BackOff = { 10, 20, 50, 100, 200, 300, 500, 600, 700, 1000 };

        public ICommandResult Process<T>(T command) where T : IMessage
        {
            var retry = 0;
            do
            {
                using var scope = new ScopedServiceProviderFactory(((IServiceProvider)factory.Get(typeof(IServiceProvider))).CreateScope());
                var logger = (ILogger<PipelineProcessor>)scope.Get(typeof(ILogger<PipelineProcessor>));
                var commandContext = (ICommandContext)scope.Get(typeof(ICommandContext));
                var securityContext = (ISecurityContext)scope.Get(typeof(ISecurityContext));
                var commandProcessor = new CommandProcessor<T>(scope, commandContext);
                var validationProcessor = new ValidationProcessor<T>(scope, commandProcessor);
                var securityProcessor = new SecurityProcessor<T>(securityContext, validationProcessor);

                try
                {
                    var commandResult = securityProcessor.Process(command);
                    return commandResult;
                }
                catch (AggregateConcurrencyException)
                {
                    commandContext.Transaction.Rollback();
                    scope.Dispose();
                    var delay = BackOff[retry++];
                    logger.LogDebug($"Retry[{retry}] for command {typeof(T).Name} with backoff delay {delay}");
                    Thread.Sleep(delay);
                }
                catch (Exception exception)
                {
                    logger?.LogError(exception, $"Command {typeof(T).FullName} error");

                    commandContext.Transaction.Rollback();
                    scope.Dispose();
                    throw;
                }

            } while (retry < RETRIES);

            throw new PipelineProcessorRetryException(command, retry);
        }
    }

    public class PipelineProcessorRetryException : Exception
    {
        public PipelineProcessorRetryException(IMessage command, int attempts) : 
            base($"Command '{command.GetType().FullName}' failed after {attempts} retries")
        {
        }
    }
}