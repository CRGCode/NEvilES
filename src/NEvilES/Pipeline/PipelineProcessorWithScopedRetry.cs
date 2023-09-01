using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public class PipelineProcessorWithScopedRetry : ICommandProcessor
    {
        private readonly IServiceScopeFactory scopeFactory;

        public PipelineProcessorWithScopedRetry(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }
        const int RETRIES = 15;
        private static readonly int[] BackOff = { 2, 20, 50, 100, 200, 300, 500, 600, 700, 1000 };
        public ICommandResult Process<T>(T command) where T : IMessage
        {
            var retry = 0;
            do
            {
                using var scope = scopeFactory.CreateScope();
                var pipelineProcessor = scope.ServiceProvider.GetRequiredService<PipelineProcessor>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<PipelineProcessorWithScopedRetry>>();
                var commandContext = scope.ServiceProvider.GetRequiredService<ICommandContext>();
                logger.LogInformation($"Processing[{command.GetStreamId()}] for {command.GetType().Name}");

                try
                {
                    var commandResult = pipelineProcessor.Process(command);
                    return commandResult;
                }
                catch (AggregateConcurrencyException)
                {
                    commandContext.Transaction.Rollback();
                    var delay = BackOff[retry++] + new Random().Next(10);
                    logger.LogInformation($"Retry[{retry}] for Command[{command.GetStreamId()}] {typeof(T).Name} with backoff delay {delay}");
                    Thread.Sleep(delay);
                }
                catch (Exception exception)
                {
                    if(!(exception is DomainAggregateException))
                        logger.LogError(exception, $"Command {typeof(T).FullName} error");

                    commandContext.Transaction?.Rollback();
                    throw;
                }

            } while (retry < RETRIES);

            throw new PipelineProcessorRetryException(command, retry);
        }

        public Task<ICommandResult> ProcessAsync<T>(T command) where T : IMessage
        {
            var retry = 0;
            do
            {
                using var scope = scopeFactory.CreateScope();
                var pipelineProcessor = scope.ServiceProvider.GetRequiredService<PipelineProcessor>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<PipelineProcessorWithScopedRetry>>();
                var context = scope.ServiceProvider.GetRequiredService<ICommandContext>();
                logger.LogInformation($"Processing[{command.GetStreamId()}] for {typeof(T).Name}");

                try
                {
                    return pipelineProcessor.ProcessAsync(command);
                }
                catch (AggregateConcurrencyException)
                {
                    context.Transaction.Rollback();
                    var delay = BackOff[retry++] + new Random().Next(10);
                    logger.LogInformation($"Retry[{retry}] for Command[{command.GetStreamId()}] {typeof(T).Name} with backoff delay {delay}");
                    Thread.Sleep(delay);
                }
                catch (Exception exception)
                {
                    if (!(exception is DomainAggregateException))
                        logger.LogError(exception, $"Command {typeof(T).FullName} error");

                    context.Transaction.Rollback();
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