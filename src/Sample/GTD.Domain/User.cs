using System;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace GTD.Domain
{
    public abstract class User
    {
        public abstract class Id : IMessage
        {
            public Guid GetStreamId() => UserId;
            public Guid UserId { get; set; }
        }

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

        public class NewUser : Id, ICommand
        {
            public Details Details { get; set; }
            public Guid ClientGroup { get; set; }

        }
        public class Created : NewUser, IEvent { }

        public class CorrectUserDetails : Id, ICommand
        {
            public Details Details { get; set; }
        }
        public class UserDetailsCorrected : CorrectUserDetails, IEvent { }

        public class Aggregate : AggregateBase,
            IHandleAggregateCommand<NewUser, UniqueNameValidator>,
            IHandleAggregateCommand<CorrectUserDetails, UniqueNameValidator>
        {
            public void Handle(NewUser command, UniqueNameValidator uniqueNameValidator)
            {
                if (uniqueNameValidator.Dispatch(command).IsValid)
                    Raise<Created>(command);
            }

            public void Handle(CorrectUserDetails command, UniqueNameValidator uniqueNameValidator)
            {
                if (uniqueNameValidator.Dispatch(command).IsValid)
                    Raise<UserDetailsCorrected>(command);
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