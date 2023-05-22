using System;
using System.Linq;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public static class ReplayEvents
    {
        public static void Replay(IFactory factory, IReadEventStore reader, long from = 0,
            long to = 0, bool writeLineException = false)
        {
            foreach (var commit in reader.Read(from, to))
            {
                var commandResult = new CommandResult(commit);
                var user = new CommandContext.User(commit.By);
                var commandContext = new CommandContext(user, null, CommandContext.User.NullUser(), null);
                try
                {
                    Project(commandResult, factory, commandContext);
                }
                catch (Exception e)
                {
                    if (writeLineException)
                    {
                        Console.WriteLine(e);
                    }
                    else
                        throw;
                }
            }
        }

        public static ICommandResult Project(ICommandResult commandResult, IFactory factory, ICommandContext commandContext)
        {
            if (!commandResult.UpdatedAggregates.Any())
            {
                return commandResult;
            }

            foreach (var agg in commandResult.UpdatedAggregates)
            {
                foreach (var message in agg.UpdatedEvents.Cast<EventData>())
                {
                    var data = new ProjectorData(agg.StreamId, commandContext, message.Type, message.Event,
                        message.TimeStamp, message.Version);
                    var projectorType = typeof(IProject<>).MakeGenericType(message.Type);
                    var projectors = factory.GetAll(projectorType);

                    // TODO below looks like it needs some DRY attention
                    foreach (var projector in projectors)
                    {
                        try
                        {
                            ((dynamic) projector).Project((dynamic) message.Event, data);
                        }
                        catch (Exception e)
                        {
                            throw new ProjectorException(e, "Projector exception {0} - {1} StreamId {2}", projector.GetType().Name, message.Event, agg.StreamId);
                        }
                    }

                    projectorType = typeof(IProjectWithResult<>).MakeGenericType(message.Type);
                    projectors = factory.GetAll(projectorType);

                    foreach (var projector in projectors)
                    {
                        try
                        {
                            IProjectorResult result = ((dynamic) projector).Project((dynamic) message.Event, data);
                            commandResult.ReadModelItems.AddRange(result.Items);
                        }
                        catch (Exception e)
                        {
                            throw new ProjectorException(e, "Projector exception {0} - {1} StreamId {2}", projector.GetType().Name, message.Event, agg.StreamId);
                        }
                    }

                }
            }

            return commandResult;
        }
    }
}