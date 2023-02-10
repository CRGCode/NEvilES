using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NEvilES.Pipeline;

namespace NEvilES.Abstractions.Pipeline
{
    public class ReadModelPipelineProcess<T> : IProcessPipelineStage<T>
       where T : IMessage
    {
        private readonly IFactory factory;
        private readonly ILogger logger;

        public ReadModelPipelineProcess(IFactory factory, ILogger logger)
        {
            this.factory = factory;
            this.logger = logger;
        }

        public ICommandResult Process(T command)
        {
            var commandContext = (ICommandContext)factory.Get(typeof(ICommandContext));
            var commandResult = (ICommandResult)factory.Get(typeof(ICommandResult));

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
                            ((dynamic)projector).Project((dynamic)message.Event, data);
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
                            IProjectorResult result = ((dynamic)projector).Project((dynamic)message.Event, data);
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

        public async Task<ICommandResult> ProcessAsync(T command)
        {
            var commandContext = (ICommandContext)factory.Get(typeof(ICommandContext));
            var commandResult = (ICommandResult)factory.Get(typeof(ICommandResult));
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
}
