using System;
using System.Collections.Generic;
using System.Text;

namespace NEvilES
{
    public class CommandValidationException : Exception
    {
        public CommandValidationException(object command, List<string> errors)
            : base(CreateErrorMessage(command,errors))
        {
            Command = command;
            Errors = errors;
        }

        public object Command { get; private set; }
        public List<string> Errors { get; private set; }

        private static string CreateErrorMessage(object command, List<string> errors)
        {
            var errorStringBuilder = new StringBuilder();

            errorStringBuilder.AppendLine();
            errors.ForEach(x => errorStringBuilder.AppendLine($"\t- {x}"));

            return $"Could not process the command '{command.GetType().FullName}' due to the following validation errors:{errorStringBuilder}";
        }
    }
}