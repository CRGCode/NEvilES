using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NEvilES.Abstractions.Pipeline
{
     public class ValidationPipelineProcessor : PipelineStage
    {
        public ValidationPipelineProcessor(IFactory factory, IProcessPipelineStage nextPipelineStage, ILogger logger) 
            : base(factory, nextPipelineStage, logger)
        {
        }

        public override ICommandResult Process<T>(T command)
        {
            var validators = Factory.GetAll(typeof(INeedExternalValidation<T>)).Cast<INeedExternalValidation<T>>().ToArray();

            if (!validators.Any())
            {
                return NextPipelineStage.Process(command);
            }

            var results = new List<CommandValidationResult>();

            foreach (var validator in validators)
            {
                try
                {
                    Logger.LogTrace($"{validator.GetType().Name}");
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
                return NextPipelineStage.Process(command);
            }

            throw new CommandValidationException(command, results.Where(x => !x.IsValid).SelectMany(x => x.Errors).ToList());
        }

        public override Task<ICommandResult> ProcessAsync<T>(T command)
        {
            var validators = Factory.GetAll(typeof(INeedExternalValidation<T>)).Cast<INeedExternalValidation<T>>().ToArray();
            if (!validators.Any())
            {
                return NextPipelineStage.ProcessAsync(command);
            }

            var results = new List<CommandValidationResult>();

            foreach (var validator in validators)
            {
                try
                {
                    Logger.LogTrace($"{validator.GetType().Name}");

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
                return NextPipelineStage.ProcessAsync(command);
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
