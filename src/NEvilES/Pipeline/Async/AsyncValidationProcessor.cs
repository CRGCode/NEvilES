using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Abstractions.Pipeline.Async;

namespace NEvilES.Pipeline.Async
{
    //public class AsyncValidationProcessor<T> : IProcessPipelineStageAsync<T>
    //    where T : IMessage
    //{
    //    private readonly IFactory factory;
    //    private readonly IProcessPipelineStageAsync<T> innerCommand;

    //    public AsyncValidationProcessor(IFactory factory, IProcessPipelineStageAsync<T> innerCommand)
    //    {
    //        this.factory = factory;
    //        this.innerCommand = innerCommand;
    //    }

    //    public Task<ICommandResult> ProcessAsync(T command)
    //    {
    //        var validators = factory.GetAll(typeof(INeedExternalValidation<T>)).Cast<INeedExternalValidation<T>>().ToArray();
    //        if (!validators.Any())
    //        {
    //            return innerCommand.ProcessAsync(command);
    //        }

    //        var results = new List<CommandValidationResult>();

    //        foreach (var validator in validators)
    //        {
    //            try
    //            {
    //                results.Add(validator.Dispatch(command));
    //            }
    //            catch (SecurityException)
    //            {
    //                throw;
    //            }
    //            catch (Exception e)
    //            {
    //                throw new ExternalCommandValidationException(e, "External Validator exception {0} - {1}", validator.GetType().Name, command.GetType().Name);
    //            }
    //        }

    //        if (results.All(x => x.IsValid))
    //        {
    //            return innerCommand.ProcessAsync(command);
    //        }

    //        throw new CommandValidationException(command, results.Where(x => !x.IsValid).SelectMany(x => x.Errors).ToList());
    //    }
    //}
}