using System;
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

            var projectResults = await ReplayEvents.ProjectAsync(result, factory, commandContext);
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
                agg.RaiseStatelessEvent((IEvent) command);
                var commit = await repo.SaveAsync(agg);
                commandResult.Append(commit);
            }
            return commandResult;
        }
    }

    public static class ReadModelProjectorHelperAsync
    {

    }


    public class CommandHandlerException : Exception
    {
        public CommandHandlerException(Exception exception, string message, params object[] args)
            : base(string.Format(message, args), exception)
        {
        }
    }
}

