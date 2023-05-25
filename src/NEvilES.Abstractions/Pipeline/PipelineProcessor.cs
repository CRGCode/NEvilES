using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NEvilES.Abstractions.Pipeline
{
    public class PipelineProcessor
    {
        private readonly IFactory factory;
        private readonly ILogger<PipelineProcessor> logger;

        public PipelineProcessor(
            IFactory factory,
            ILogger<PipelineProcessor> logger)
        {
            this.factory = factory;
            this.logger = logger;
        }

        public ICommandResult Process<T>(T command) where T : IMessage
        {
            var pipeline =  CreatePipelineFor(factory, logger);

            logger.LogTrace($"Processing[{command.GetStreamId()}]");
            var commandResult = pipeline.Process(command);
            return commandResult;
        }

        public Task<ICommandResult> ProcessAsync<T>(T command) where T : IMessage
        {
            var pipeline = CreatePipelineFor(factory, logger);

            logger.LogTrace($"Processing[{command.GetStreamId()}]");
            var commandResult = pipeline.ProcessAsync(command);
            return commandResult;
        }

        private static List<ConstructorInfo> _ctors = new List<ConstructorInfo>();
        public static void AddStage(Type stageType)
        {
            _ctors.Insert(0, stageType.GetConstructor(new[] { typeof(IFactory), typeof(IProcessPipelineStage), typeof(ILogger) }));
        }

        private static IProcessPipelineStage CreatePipelineFor(IFactory factory, ILogger logger)
        {
            IProcessPipelineStage nextStage = null;
            foreach (var ctor in _ctors)
            {
                var stage = (IProcessPipelineStage)ctor.Invoke(new object[] { factory, nextStage, logger });
                nextStage = stage;
            }

            return nextStage;
        }
    }
}