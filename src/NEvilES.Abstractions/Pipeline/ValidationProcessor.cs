using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NEvilES.Abstractions.Pipeline
{
     public class ValidationProcessor<T> : IProcessPipelineStage<T>
        where T : IMessage
    {
        private readonly IFactory factory;
        private readonly IProcessPipelineStage<T> innerCommand;
        private readonly ILogger logger;

        public ValidationProcessor(IFactory factory, IProcessPipelineStage<T> innerCommand, ILogger logger)
        {
            this.factory = factory;
            this.innerCommand = innerCommand;
            this.logger = logger;
        }

        public ICommandResult Process(T command)
        {
            var validators = factory.GetAll(typeof(INeedExternalValidation<T>)).Cast<INeedExternalValidation<T>>().ToArray();

            if (!validators.Any())
            {
                return innerCommand.Process(command);
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
                return innerCommand.Process(command);
            }

            throw new CommandValidationException(command, results.Where(x => !x.IsValid).SelectMany(x => x.Errors).ToList());
        }

        public Task<ICommandResult> ProcessAsync(T command)
        {
            var validators = factory.GetAll(typeof(INeedExternalValidation<T>)).Cast<INeedExternalValidation<T>>().ToArray();
            if (!validators.Any())
            {
                return innerCommand.ProcessAsync(command);
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
                return innerCommand.ProcessAsync(command);
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
