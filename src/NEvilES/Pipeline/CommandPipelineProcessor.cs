using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public class CommandPipelineProcessor : PipelineStage
    {
        public CommandPipelineProcessor(IFactory factory, IProcessPipelineStage nextPipelineStage, ILogger logger) 
            : base(factory, nextPipelineStage, logger)
        {
        }

        public override ICommandResult Process<TCommand>(TCommand command) 
        {
            var result = Execute(command);
            return NextPipelineStage == null ? result : NextPipelineStage.Process(command);
        }

        public override async Task<ICommandResult> ProcessAsync<TCommand>(TCommand command)
        {
            var result = await ExecuteAsync(command);
            if (NextPipelineStage == null)
            {
                return result;
            }

            return await NextPipelineStage.ProcessAsync(command);
        }

        public ICommandResult Execute<T>(T message) where T : IMessage
        {
            var commandType = message.GetType();
            var repo = (IRepository)Factory.Get(typeof(IRepository));
            var commandResult = (ICommandResult)Factory.Get(typeof(ICommandResult));

            if (message is ICommand command)
            {
                var streamId = command.GetStreamId();
                var type = typeof(IHandleAggregateCommandMarker<>).MakeGenericType(commandType);

                var aggHandlers = Factory.GetAll(type).Cast<IAggregateHandlers>().ToList();

                if (aggHandlers.Any())
                {
                    var agg = repo.Get(aggHandlers.First().GetType(), streamId);
                    // Check ICreationCommands are the first command
                    if (agg.Version == 0)
                    {
                        if (!(command is ICreationCommand))
                            throw new Exception("Missing Creation command");
                    }
                    else
                    {
                        // Check ICreationCommands are only executed once on the aggregate
                        if (command is ICreationCommand)
                            throw new Exception("Can't run Creation command after Aggregate has been already created");
                    }
                    var aggHandler = aggHandlers.SingleOrDefault(x => x.GetType() == agg.GetType());

                    if (aggHandler == null)
                    {
                        throw new Exception(
                            $"Possible attempt to create stream from abstract aggregate with command: {commandType}");
                    }

                    var handler = aggHandler.Handlers[commandType];
                    var parameters = handler.GetParameters().Skip(1);  // skip the command

                    var dependencies = new object[] { command }
                        .Concat(parameters.Select(x =>
                        {
                            var param = Factory.Get(x.ParameterType);
                            if(param != null)
                                return param;
                            throw new MissingHandlerDependency(handler, x.ParameterType);
                        })).ToArray();

                    var aggName = agg.GetType().ReflectedType?.Name ?? agg.GetType().Name;
                    var p = string.Join(',', dependencies.Select(x => x.GetType().Name).Skip(1));
                    Logger.LogTrace($"{aggName}.Handle<{commandType.Name}>({p})");
                    try
                    {
                        handler.Invoke(agg, dependencies);
                    }
                    catch (TargetInvocationException e) when (e.InnerException != null)
                    {
                        if (!(e.InnerException is DomainAggregateException))
                        {
                            Logger.LogError(e, $"Error {e.Message}");
                        }
                        throw e.InnerException;
                    }
                    catch (Exception e)
                    {
                        Logger.LogTrace(e, $"Error {e.Message}");
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
                var commandHandlers = Factory.GetAll(commandProcessorType).Cast<object>().ToArray();

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
                var singleAggHandler = Factory.TryGet(type);
                var streamId = message.GetStreamId();
                Logger.LogTrace($"IHandleStatelessEvent<{commandType.Name}>");

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
            var repo = (IAsyncRepository)Factory.Get(typeof(IAsyncRepository));

            if (message is ICommand command)
            {
                var streamId = command.GetStreamId();
                var type = typeof(IHandleAggregateCommandMarker<>).MakeGenericType(commandType);
                var aggHandlers = Factory.GetAll(type).Cast<IAggregateHandlers>().ToList();
                if (aggHandlers.Any())
                {
                    var agg = await repo.GetAsync(aggHandlers.First().GetType(), streamId);
                    Logger.LogTrace($"GetAsync<{commandType.Name}>");
                    var aggHandler = aggHandlers.SingleOrDefault(x => x.GetType() == agg.GetType());

                    if (aggHandler == null)
                    {
                        throw new Exception(
                            $"Possible attempt to create stream from abstract aggregate with command: {commandType}");
                    }

                    var handler = aggHandler.Handlers[commandType];
                    var parameters = handler.GetParameters();
                    var deps = new object[] { command }
                        .Concat(parameters.Skip(1).Select(x => Factory.Get(x.ParameterType))).ToArray();

                    Logger.LogTrace($"{agg.GetType().ReflectedType?.Name ?? agg.GetType().Name}.Handle<{commandType.Name}>({string.Join(',', deps.Select(x => x.GetType().Name).Skip(1))})");

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
                var commandHandlers = Factory.GetAll(commandProcessorType).Cast<object>().ToArray();

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
                    Logger.LogTrace($"commandHandler<{commandHandler}>");

                    try
                    {
                        await (Task)method!.Invoke(commandHandler, new object[] { message });
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"IProcessCommandAsync<{commandType.Name}> Error - {e.Message}>");

                        throw;
                    }
     
                }
            }
            else
            {
                var type = typeof(IHandleStatelessEvent<>).MakeGenericType(commandType);
                var singleAggHandler = Factory.TryGet(type);

                var agg = await repo.GetStatelessAsync(singleAggHandler?.GetType(), ((IEvent)message).GetStreamId());
                // TODO don't like the cast below of message to IEvent
                agg.RaiseStatelessEvent((IEvent)message);
                var commit = await repo.SaveAsync(agg);
                commandResult.Append(commit);
            }

            return commandResult;
        }
    }

    public class MissingHandlerDependency : Exception
    {
        public MissingHandlerDependency(MethodInfo handler, Type dependencyType) : base($"Command Handler {handler} missing dependency {dependencyType.Name}")
        {
           
        }
    }
}

