using System;
using NEvilES;
using NEvilES.Abstractions;

namespace GTD.Domain
{
    public abstract class User
    {
        public class Details
        {
            public Details(string email, string password, string role, string name, string username = null)
            {
                Email = email;
                Password = password;
                Role = role;
                Name = name;
                Username = username ?? email;
            }

            public string Email { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
            public string Name { get; set; }
        }

        public class NewUser : Created, ICommand { }
        public class Created : Event
        {
            public Details Details { get; set; }
            public Guid ClientGroup { get; set; }
        }

        public class CorrectUserDetails : UserDetailsCorrected, ICommand { }
        public class UserDetailsCorrected : Event
        {
            public Details Details { get; set; }
        }

        public class Aggregate : AggregateBase,
            IHandleAggregateCommand<NewUser, UniqueNameValidator>,
            IHandleAggregateCommand<CorrectUserDetails, UniqueNameValidator>
        {
            public ICommandResponse Handle(NewUser command, UniqueNameValidator uniqueNameValidator)
            {
                if (uniqueNameValidator.Dispatch(command).IsValid)
                {
                    RaiseEvent<Created>(command);
                    return new CommandCompleted(command.StreamId, nameof(NewUser));
                }
                return new CommandRejectedWithError<string>(command.StreamId, nameof(NewUser),"");
            }

            public ICommandResponse Handle(CorrectUserDetails command, UniqueNameValidator uniqueNameValidator)
            {
                if (uniqueNameValidator.Dispatch(command).IsValid)
                {
                    RaiseEvent<UserDetailsCorrected>(command);
                    return new CommandCompleted(command.StreamId, nameof(CorrectUserDetails));
                }
                return new CommandRejectedWithError<string>(command.StreamId, nameof(CorrectUserDetails),"");
            }

            //-------------------------------------------------------------------

            private void Apply(Created e)
            {
            }
        }

        public class UniqueNameValidator :
            INeedExternalValidation<NewUser>,
            INeedExternalValidation<CorrectUserDetails>
        {
            public CommandValidationResult Dispatch(NewUser command)
            {
                return Validate(command.Details);
            }

            public CommandValidationResult Dispatch(CorrectUserDetails command)
            {
                return Validate(command.Details);
            }

            private CommandValidationResult Validate(Details commandDetails)
            {
                // Check ReadModel
                return new CommandValidationResult(true);
            }
        }
    }
}