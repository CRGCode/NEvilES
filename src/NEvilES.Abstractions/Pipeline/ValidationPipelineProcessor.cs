using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NEvilES.Abstractions.Pipeline
{
     public class ValidationPipelineProcessor<T> : IProcessPipelineStage<T>
        where T : IMessage
    {
        private readonly IFactory factory;
        private readonly IProcessPipelineStage<T> nextPipelineStage;
        private readonly ILogger logger;

        public ValidationPipelineProcessor(IFactory factory, IProcessPipelineStage<T> nextPipelineStage, ILogger logger)
        {
            this.factory = factory;
            this.nextPipelineStage = nextPipelineStage;
            this.logger = logger;
        }

        public ICommandResult Process(T command)
        {
            var validators = factory.GetAll(typeof(INeedExternalValidation<T>)).Cast<INeedExternalValidation<T>>().ToArray();

            if (!validators.Any())
            {
                return nextPipelineStage.Process(command);
            }

            var results = new List<CommandValidationResult>();

            foreach (var validator in validators)
            {
                try
                {
                    logger.LogTrace($"{validator.GetType().Name}");
                    results.Add(validator.Dispatch(command));
                }
                catch (SecurityException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new ExternalCommandValidationException(e, "External Validator exception {0} - {1}", validator.GetType().Name, command.GetType().Name);
                }
            }

            if (results.All(x => x.IsValid))
            {
                return nextPipelineStage.Process(command);
            }

            throw new CommandValidationException(command, results.Where(x => !x.IsValid).SelectMany(x => x.Errors).ToList());
        }

        public Task<ICommandResult> ProcessAsync(T command)
        {
            var validators = factory.GetAll(typeof(INeedExternalValidation<T>)).Cast<INeedExternalValidation<T>>().ToArray();
            if (!validators.Any())
            {
                return nextPipelineStage.ProcessAsync(command);
            }

            var results = new List<CommandValidationResult>();

            foreach (var validator in validators)
            {
                try
                {
                    logger.LogTrace($"{validator.GetType().Name}");

                    results.Add(validator.Dispatch(command));
                }
                catch (SecurityException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new ExternalCommandValidationException(e, "External Validator exception {0} - {1}", validator.GetType().Name, command.GetType().Name);
                }
            }

            if (results.All(x => x.IsValid))
            {
                return nextPipelineStage.ProcessAsync(command);
            }

            throw new CommandValidationException(command, results.Where(x => !x.IsValid).SelectMany(x => x.Errors).ToList());
        }
    }

    public class ExternalCommandValidationException : Exception
    {
        public ExternalCommandValidationException(Exception exception, string message, params object[] args)
            : base(string.Format(message, args), exception)
        {
        }
    }
}
