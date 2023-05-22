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
        const int RETRIES = 10;
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
                logger.LogInformation($"Processing[{command.GetStreamId()}] for {typeof(T).Name}");

                try
                {
                    var commandResult = pipelineProcessor.Process(command);
                    return commandResult;
                }
                catch (AggregateConcurrencyException)
                {
                    commandContext.Transaction.Rollback();
                    //scope.Dispose();
                    var delay = BackOff[retry++] + new Random().Next(10);
                    logger.LogInformation($"Retry[{retry}] for Command[{command.GetStreamId()}] {typeof(T).Name} with backoff delay {delay}");
                    Thread.Sleep(delay);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, $"Command {typeof(T).FullName} error");

                    commandContext.Transaction.Rollback();
                    //scope.Dispose();
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
                var commandProcessor = scope.ServiceProvider.GetRequiredService<PipelineProcessor>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<PipelineProcessorWithScopedRetry>>();
                var context = scope.ServiceProvider.GetRequiredService<ICommandContext>();
                logger.LogInformation($"Processing[{command.GetStreamId()}] for {typeof(T).Name}");

                try
                {
                    return commandProcessor.ProcessAsync(command);
                }
                catch (AggregateConcurrencyException)
                {
                    context.Transaction.Rollback();
                    //scope.Dispose();
                    var delay = BackOff[retry++] + new Random().Next(10);
                    logger.LogInformation($"Retry[{retry}] for Command[{command.GetStreamId()}] {typeof(T).Name} with backoff delay {delay}");
                    Thread.Sleep(delay);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, $"Command {typeof(T).FullName} error");

                    context.Transaction.Rollback();
                    //scope.Dispose();
                    throw;
                }

            } while (retry < RETRIES);

            throw new PipelineProcessorRetryException(command, retry);
        }
    }

    public class PipelineProcessor
    {
        private readonly ISecurityContext securityContext;
        private readonly IFactory factory;
        private readonly ILogger<PipelineProcessor> logger;

        public PipelineProcessor(
            ISecurityContext securityContext,
            IFactory factory,
            ILogger<PipelineProcessor> logger)
        {
            this.securityContext = securityContext;
            this.factory = factory;
            this.logger = logger;
        }

        public ICommandResult Process<T>(T command) where T : IMessage
        {
            var readModelProcessor = new ReadModelPipelineProcess<T>(factory, logger);
            var commandProcessor = new CommandPipelineProcessor<T>(factory, readModelProcessor, logger);
            var validationProcessor = new ValidationPipelineProcessor<T>(factory, commandProcessor, logger);

            logger.LogTrace($"Processing[{command.GetStreamId()}]");
            var commandResult = validationProcessor.Process(command);
            return commandResult;
        }

        public Task<ICommandResult> ProcessAsync<T>(T command) where T : IMessage
        {
            var readModelProcessor = new ReadModelPipelineProcess<T>(factory, logger);
            var commandProcessor = new CommandPipelineProcessor<T>(factory, readModelProcessor, logger);
            var validationProcessor = new ValidationPipelineProcessor<T>(factory, commandProcessor, logger);

            logger.LogTrace($"Processing[{command.GetStreamId()}]");
            var commandResult = validationProcessor.ProcessAsync(command);
            return commandResult;
        }

        public static void AddStage<TCommand>(IProcessPipelineStage<TCommand> stage)
            where TCommand : IMessage
        {
            // WIP
            // Something like below would be nice or maybe how .NET core does it http pipeline
            /*
            services.AddCamundaWorker("sampleWorker")
                .AddHandler<SayHelloHandler>()
                .AddHandler<SayHelloGuestHandler>()
                .ConfigurePipeline(pipeline =>
                {
                    pipeline.Use(next => async context =>
                    {
                        var logger = context.ServiceProvider.GetRequiredService<ILogger<Startup>>();
                        logger.LogInformation("Started processing of task {Id}", context.Task.Id);
                        await next(context);
                        logger.LogInformation("Finished processing of task {Id}", context.Task.Id);
                    });
                });
            */
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