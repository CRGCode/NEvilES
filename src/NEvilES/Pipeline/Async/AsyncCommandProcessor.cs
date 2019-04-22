using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Abstractions.Pipeline.Async;

namespace NEvilES.Pipeline.Async
{
    public class AsyncCommandProcessor<TCommand> : IProcessPipelineStageAsync<TCommand>
        where TCommand : IMessage
    {
        private readonly IFactory factory;
        private readonly ICommandContext commandContext;

        public AsyncCommandProcessor(IFactory factory, ICommandContext commandContext)
        {
            this.factory = factory;
            this.commandContext = commandContext;
        }

        public virtual async Task<ICommandResult> ProcessAsync(TCommand command)
        {
            var result = await ExecuteAsync(command);

            var projectResults = await ReadModelProjectorHelperAsync.ProjectAsync(result, factory, commandContext);
            return commandContext.Result.Add(projectResults);
        }

        public async Task<CommandResult> ExecuteAsync<T>(T command) where  T : IMessage
        {
            var commandResult = new CommandResult();
            var commandType = command.GetType();
            var streamId = command.StreamId;
            var repo = (IAsyncRepository) factory.Get(typeof(IAsyncRepository));

            if (command is ICommand)
            {
                var type = typeof(IHandleAggregateCommandMarker<>).MakeGenericType(commandType);
                var aggHandlers = factory.GetAll(type).Cast<IAggregateHandlers>().ToList();
                if (aggHandlers.Any())
                {
                    var agg = await repo.GetAsync(aggHandlers.First().GetType(), streamId);
                    var aggHandler = aggHandlers.SingleOrDefault(x => x.GetType() == agg.GetType());

                    if (aggHandler == null)
                    {
                        throw new Exception($"Possible attempt to create stream from abstract aggregate with command: {commandType}");
                    }

                    var handler = aggHandler.Handlers[commandType];
                    var parameters = handler.GetParameters();
                    var deps = new object[] {command}.Concat(parameters.Skip(1).Select(x => factory.Get(x.ParameterType))).ToArray();

                    try
                    {
                        handler.Invoke(agg, deps);
                    }
                    catch (TargetInvocationException e) when (e.InnerException != null)
                    {
                        throw e.InnerException;
                    }
                    var commit = await repo.SaveAsync(agg);
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
                    commandHandler.Handle(command);
                }
                // Version using reflection
                //foreach (var commandHandler in commandHandlers)
                //{
                //    var method = commandHandler.GetType().GetMethod("Handle");
                //    method.Invoke(commandHandler, new object[] { command });
                //}
            }
            else
            {
                var type = typeof(IHandleStatelessEvent<>).MakeGenericType(commandType);
                var singleAggHandler = factory.TryGet(type);

                var agg = await repo.GetStatelessAsync(singleAggHandler?.GetType(), streamId);
                // // TODO don't like the cast below of command to IEvent
                agg.RaiseStateless((IEvent) command);
                var commit = await repo.SaveAsync(agg);
                commandResult.Append(commit);
            }
            return commandResult;
        }
    }

    public static class ReadModelProjectorHelperAsync
    {
        public static async Task<ICommandResult> ProjectAsync(ICommandResult commandResult, IFactory factory, ICommandContext commandContext)
        {
            if (!commandResult.UpdatedAggregates.Any())
            {
                return commandResult;
            }

            foreach (var agg in commandResult.UpdatedAggregates)
            {
                foreach (var message in agg.UpdatedEvents.Cast<EventData>())
                {
                    var data = new ProjectorData(agg.StreamId, commandContext, message.Type, message.Event, message.TimeStamp, message.Version);
                    var projectorType = typeof(IProjectAsync<>).MakeGenericType(message.Type);
                    var projectors = factory.GetAll(projectorType);

                    // TODO below looks like it needs some DRY attention
                    foreach (var projector in projectors)
                    {
#if !DEBUG
                        try
                        {
#endif
                            await ((dynamic)projector).ProjectAsync((dynamic)message.Event, data);
#if !DEBUG
                        }
                        catch (Exception e)
                        {
                            throw new ProjectorException(e, "Projector exception {0} - {1} StreamId {2}", projector.GetType().Name, message.Event, agg.StreamId);
                        }
#endif
                    }

                    projectorType = typeof(IProjectWithResultAsync<>).MakeGenericType(message.Type);
                    projectors = factory.GetAll(projectorType);

                    foreach (var projector in projectors)
                    {
#if !DEBUG
                        try
                        {
#endif
                        ProjectorResult result = await ((dynamic)projector).ProjectAsync((dynamic)message.Event, data);
                        commandResult.ReadModelItems.AddRange(result.Items);
#if !DEBUG
                        }
                        catch (Exception e)
                        {
                            throw new ProjectorException(e, "Projector exception {0} - {1} StreamId {2}", projector.GetType().Name, message.Event, agg.StreamId);
                        }
#endif
                    }

                }
            }
            return commandResult;
        }

    }


    public class CommandHandlerException : Exception
    {
        public CommandHandlerException(Exception exception, string message, params object[] args)
            : base(string.Format(message, args), exception)
        {
        }
    }
}

