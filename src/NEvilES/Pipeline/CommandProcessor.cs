using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NEvilES.Pipeline
{
    public class CommandProcessor<TCommand> : IProcessPipelineStage<TCommand>
        where TCommand : IMessage
    {
        private readonly IFactory factory;
        private readonly CommandContext commandContext;
        private readonly INeedApproval approver;

        public CommandProcessor(IFactory factory, CommandContext commandContext, INeedApproval approver)
        {
            this.factory = factory;
            this.commandContext = commandContext;
            this.approver = approver;
        }

        public CommandResult Process(TCommand command)
        {
            CommandResult result;
            if (commandContext.ApprovalContext != null)
            {
                var approvalContext = commandContext.ApprovalContext;
                switch (approvalContext.Perform)
                {
                    case ApprovalContext.Action.Request:
                        result = new CommandResult(approver.Capture(command));
                        break;
                    case ApprovalContext.Action.Approve:
                        var approvalResult = approver.Approve(command.StreamId);
                        var method = GetType().GetTypeInfo().GetMethod("Execute");
                        var genericMethod = method.MakeGenericMethod(approvalResult.Command.GetType());

                        commandContext.ApprovalContext = null;
                        result = (CommandResult) genericMethod.Invoke(this, new [] { approvalResult.Command });
                        result.Append(approvalResult.Commit);
                        break;
                    case ApprovalContext.Action.Decline:
                        result = Execute(command);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                result = Execute(command);
            }

            var projectResults = ReadModelProjectorHelper.Project(result, factory, commandContext);
            return commandContext.Result.Add(projectResults);
        }

        public CommandResult Execute<T>(T command) where  T : IMessage
        {
            var commandResult = new CommandResult();
            var commandType = command.GetType();
            var streamId = command.StreamId;
            var repo = (IRepository) factory.Get(typeof(IRepository));

            if (command is ICommand)
            {
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
                    var deps = new object[] {command}.Concat(parameters.Skip(1).Select(x => factory.Get(x.ParameterType))).ToArray();

                    try
                    {
                        handler.Invoke(agg, deps);
                    }
                    catch (TargetInvocationException e) when (e.InnerException != null)
                    {
                        throw e.InnerException;
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

                var agg = repo.GetStateless(singleAggHandler?.GetType(), streamId);
                // TODO don't like the cast below of command to IEvent
                agg.RaiseStateless((IEvent) command);
                var commit = repo.Save(agg);
                commandResult.Append(commit);
            }
            return commandResult;
        }
    }

    public static class ReadModelProjectorHelper
    {
        public static CommandResult Project(CommandResult commandResult, IFactory factory, CommandContext commandContext)
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
                    var projectorType = typeof(IProject<>).MakeGenericType(message.Type);
                    var projectors = factory.GetAll(projectorType);

                    // TODO below looks like it needs some DRY attention
                    foreach (var projector in projectors)
                    {
#if !DEBUG
                        try
                        {
#endif
                            ((dynamic)projector).Project((dynamic)message.Event, data);
#if !DEBUG
                        }
                        catch (Exception e)
                        {
                            throw new ProjectorException(e, "Projector exception {0} - {1} StreamId {2}", projector.GetType().Name, message.Event, agg.StreamId);
                        }
#endif
                    }

                    projectorType = typeof(IProjectWithResult<>).MakeGenericType(message.Type);
                    projectors = factory.GetAll(projectorType);

                    foreach (var projector in projectors)
                    {
#if !DEBUG
                        try
                        {
#endif
                        ProjectorResult result = ((dynamic)projector).Project((dynamic)message.Event, data);
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

    public class ProjectorException : Exception
    {
        public ProjectorException(Exception exception, string message, params object[] args)
            : base(string.Format(message, args), exception)
        {
        }
    }
}

