using System;
using System.Collections.Generic;
using System.Linq;

namespace NEvilES
{
    public abstract class CommandValidatorBase<T> : INeedExternalValidation<T>
    {
        CommandValidationResult INeedExternalValidation<T>.Dispatch(T command)
        {
            return Dispatch(command);
        }

        protected abstract CommandValidationResult Dispatch(T command);
    }

    // TODO rename this
    public interface INeedExternalValidation<in T>
    {
        CommandValidationResult Dispatch(T command);
    }

    public class CommandValidationResult
    {
        public bool IsValid { get; }
        public List<string> Errors { get; } = new List<string>();

        public CommandValidationResult()
        {
            IsValid = true;
        }

        public CommandValidationResult(params string[] errors)
        {
            Errors.AddRange(errors);
        }

        public CommandValidationResult(bool result, params string[] error)
        {
            IsValid = result;
            if (!result)
            {
                Errors.AddRange(error);
            }
        }

        public static CommandValidationResult All(params Func<CommandValidationResult>[] results)
        {
            return results.Aggregate(new CommandValidationResult(), (current, result) => current + result());
        }

        public static CommandValidationResult Any(params Func<CommandValidationResult>[] results)
        {
            foreach (var result in results)
            {
                var commandValidationResult = result();
                if (!commandValidationResult.IsValid)
                {
                    return commandValidationResult;
                }
            }
            return new CommandValidationResult();
        }

        public static CommandValidationResult operator +(CommandValidationResult c1, CommandValidationResult c2)
        {
            return new CommandValidationResult(c1.IsValid && c2.IsValid, c1.Errors.Concat(c2.Errors).ToArray());
        }
    }
}
