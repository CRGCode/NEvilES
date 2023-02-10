using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public class CommandPipelineProcessor<TCommand> : IProcessPipelineStage<TCommand>
        where TCommand : IMessage
    {
        private readonly IFactory factory;
        private readonly IProcessPipelineStage<TCommand> nextPipelineStage;
        private readonly ILogger logger;

        public CommandPipelineProcessor(IFactory factory, IProcessPipelineStage<TCommand> nextPipelineStage, ILogger logger)
        {
            this.factory = factory;
            this.nextPipelineStage = nextPipelineStage;
            this.logger = logger;
        }

        public virtual ICommandResult Process(TCommand command)
        {
            var result = Execute(command);
            if (nextPipelineStage == null)
            {
                return result;
            }
                
            return nextPipelineStage.Process(command);
        }

        public virtual async Task<ICommandResult> ProcessAsync(TCommand command)
        {
            var result = await ExecuteAsync(command);
            if (nextPipelineStage == null)
            {
                return result;
            }

            return await nextPipelineStage.ProcessAsync(command);
        }

        public ICommandResult Execute<T>(T message) where T : IMessage
        {
            var commandType = message.GetType();
            var repo = (IRepository)factory.Get(typeof(IRepository));
            var commandResult = (ICommandResult)factory.Get(typeof(ICommandResult));

            if (message is ICommand command)
            {
                var streamId = command.GetStreamId();
                var type = typeof(IHandleAggregateCommandMarker<>).MakeGenericType(commandType);

                var aggHandlers = factory.GetAll(type).Cast<IAggregateHandlers>().ToList();

                if (aggHandlers.Any())
                {
                    var agg = repo.Get(aggHandlers.First().GetType(), streamId);
                    var aggHandler = aggHandlers.SingleOrDefault(x => x.GetType() == agg.GetType());

                    if (aggHandler == null)
                    {
                        throw new Exception(
                            $"Possible attempt to create stream from abstract aggregate with command: {commandType}");
                    }

                    var handler = aggHandler.Handlers[commandType];
                    var parameters = handler.GetParameters();
                    var dependencies = new object[] { command }
                        .Concat(parameters.Skip(1).Select(x => factory.Get(x.ParameterType))).ToArray();

                    logger.LogTrace($"{agg.GetType().ReflectedType?.Name ?? agg.GetType().Name}.Handle<{commandType.Name}>({string.Join(',', dependencies.Select(x => x.GetType().Name).Skip(1))})");
                    try
                    {
                        handler.Invoke(agg, dependencies);
                    }
                    catch (TargetInvocationException e) when (e.InnerException != null)
                    {
                        logger.LogError(e, $"Error {e.Message}");
                        throw e.InnerException;
                    }
                    catch (Exception e)
                    {
                        logger.LogTrace(e, $"Error {e.Message}");
                        throw;
                    }

                    var commit = repo.Save(agg);
                    commandResult.Append(commit);
                }
                else
                {
                    // not sure what happen with commandResult as it will be empty and below handlers don't change this?
                }

                var commandProcessorType = typeof(IHandleCommand<>).MakeGenericType(commandType);
                var commandHandlers = factory.GetAll(commandProcessorType).Cast<object>().ToArray();

                if (!aggHandlers.Any() && !commandHandlers.Any())
                {
                    throw new Exception(
                        $"Cannot find a matching IHandleAggregateCommand<> or IHandleCommand<> for {commandType}");
                }

                foreach (dynamic commandHandler in commandHandlers)
                {
                    commandHandler.Handle(message);
                }
                // Version using reflection
                //foreach (var commandHandler in commandHandlers)
                //{
                //    var method = commandHandler.GetType().GetMethod("Handle");
                //    method.Invoke(commandHandler, new object[] { message });
                //}
                
            }
            else
            {
                var type = typeof(IHandleStatelessEvent<>).MakeGenericType(commandType);
                var singleAggHandler = factory.TryGet(type);
                var streamId = message.GetStreamId();
                logger.LogTrace($"IHandleStatelessEvent<{commandType.Name}>");

                var agg = repo.GetStateless(singleAggHandler?.GetType(), streamId);
                // // TODO don't like the cast below of message to IEvent
                agg.RaiseStatelessEvent((IEvent)message);
                var commit = repo.Save(agg);
                commandResult.Append(commit);
            }

            return commandResult;
        }

        public async Task<CommandResult> ExecuteAsync<T>(T message) where T : IMessage
        {
            var commandResult = new CommandResult();
            var commandType = message.GetType();
            var repo = (IAsyncRepository)factory.Get(typeof(IAsyncRepository));

            if (message is ICommand command)
            {
                var streamId = command.GetStreamId();
                var type = typeof(IHandleAggregateCommandMarker<>).MakeGenericType(commandType);
                var aggHandlers = factory.GetAll(type).Cast<IAggregateHandlers>().ToList();
                if (aggHandlers.Any())
                {
                    var agg = await repo.GetAsync(aggHandlers.First().GetType(), streamId);
                    logger.LogTrace($"GetAsync<{commandType.Name}>");
                    var aggHandler = aggHandlers.SingleOrDefault(x => x.GetType() == agg.GetType());

                    if (aggHandler == null)
                    {
                        throw new Exception(
                            $"Possible attempt to create stream from abstract aggregate with command: {commandType}");
                    }

                    var handler = aggHandler.Handlers[commandType];
                    var parameters = handler.GetParameters();
                    var deps = new object[] { command }
                        .Concat(parameters.Skip(1).Select(x => factory.Get(x.ParameterType))).ToArray();

                    logger.LogTrace($"{agg.GetType().ReflectedType?.Name ?? agg.GetType().Name}.Handle<{commandType.Name}>({string.Join(',', deps.Select(x => x.GetType().Name).Skip(1))})");

                    try
                    {
                        handler.Invoke(agg, deps);
                        //await (Task)handler.Invoke(agg, deps);
                    }
                    catch (TargetInvocationException e) when (e.InnerException != null)
                    {
                        throw e.InnerException;
                    }

                    var commit = await repo.SaveAsync(agg);
                    commandResult.Append(commit);
                }


                var commandProcessorType = typeof(IProcessCommandAsync<>).MakeGenericType(commandType);
                var commandHandlers = factory.GetAll(commandProcessorType).Cast<object>().ToArray();

                if (!aggHandlers.Any() && !commandHandlers.Any())
                {
                    throw new Exception(
                        $"Cannot find a matching IHandleAggregateCommand<> or IProcessCommandAsync<> for {commandType}");
                }

                //foreach (var commandHandler in commandHandlers)
                //{

                //    Task result = (Task)commandHandler.HandleAsync(command);
                //    await result;
                //    //await commandHandler.HandleAsync(command);
                //}
                //Version using reflection
                foreach (var commandHandler in commandHandlers)
                {
                    var method = commandHandler.GetType().GetMethod("HandleAsync");
                    logger.LogTrace($"commandHandler<{commandHandler}>");

                    try
                    {
                        await (Task)method!.Invoke(commandHandler, new object[] { message });
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"IProcessCommandAsync<{commandType.Name}> Error - {e.Message}>");

                        throw;
                    }
     
                }
            }
            else
            {
                var type = typeof(IHandleStatelessEvent<>).MakeGenericType(commandType);
                var singleAggHandler = factory.TryGet(type);

                var agg = await repo.GetStatelessAsync(singleAggHandler?.GetType(), ((IEvent)message).GetStreamId());
                // TODO don't like the cast below of message to IEvent
                agg.RaiseStatelessEvent((IEvent)message);
                var commit = await repo.SaveAsync(agg);
                commandResult.Append(commit);
            }

            return commandResult;
        }
    }
}

