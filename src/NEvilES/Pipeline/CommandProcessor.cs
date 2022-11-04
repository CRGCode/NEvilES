using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public class CommandProcessor<TCommand> : IProcessPipelineStage<TCommand>
        where TCommand : IMessage
    {
        private readonly IFactory factory;
        private readonly ICommandContext commandContext;
        private readonly ILogger logger;

        public CommandProcessor(IFactory factory, ICommandContext commandContext, ILogger logger) 
        {
            this.factory = factory;
            this.commandContext = commandContext;
            this.logger = logger;
        }

        public virtual ICommandResult Process(TCommand command)
        {
            var result = Execute(command);

            var projectResults = ReplayEvents.Project(result, factory, commandContext);
            return commandContext.Result.Add(projectResults);
        }

        public ICommandResult Execute<T>(T message) where T : IMessage
        {
            var commandResult = new CommandResult();
            var commandType = message.GetType();
            var repo = (IRepository)factory.Get(typeof(IRepository));

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
                        throw new Exception($"Possible attempt to create stream from abstract aggregate with command: {commandType}");
                    }

                    var handler = aggHandler.Handlers[commandType];
                    var parameters = handler.GetParameters();
                    var dependencies = new object[] { command }.Concat(parameters.Skip(1).Select(x => factory.Get(x.ParameterType))).ToArray();

                    logger.LogTrace($"{agg.GetType().ReflectedType?.Name ?? agg.GetType().Name }.Handle<{commandType.Name}>({string.Join(',',dependencies.Select(x => x.GetType().Name).Skip(1))})");
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

                var commandProcessorType = typeof(IProcessCommand<>).MakeGenericType(commandType);
                var commandHandlers = factory.GetAll(commandProcessorType).Cast<object>().ToArray();

                if (!aggHandlers.Any() && !commandHandlers.Any())
                {
                    throw new Exception($"Cannot find a matching IHandleAggregateCommand<> or IProcessCommand<> for {commandType}");
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
    }
}

