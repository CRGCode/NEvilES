using System;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class Customer
    {
        public abstract class Id : IMessage
        {
            public Guid GetStreamId() => CustomerId;
            public Guid CustomerId { get; set; }
        }

        public class Create : Id, ICommand
        {
            public PersonalDetails Details { get; set; }
        }

        public class Created : Create, IEvent { }

        public class SendInvite : Id, ICommand
        {
            public SendInvite(Guid id, PersonalDetails details, string email)
            {
                CustomerId = id;
                Details = details;
                Email = email;
            }

            public PersonalDetails Details { get; set; }
            public string Email { get; set; }
        }
        public class Complain : Id, ICommand
        {
            public string Reason { get; set; }
        }

        public class Complaint : Complain, IEvent { }

        public class Refunded : Refund, IEvent
        {
        }

        public class Refund : Id, ICommand
        {
            public decimal Amount { get; set; }
            public decimal Reason { get; set; }
        }

        public class EmailSent : Id, IEvent
        {
            public string Text { get; set; }
        }

        public class NoteAdded : Id, IEvent
        {
            public string Text { get; set; }
        }

        public class BadStatelessEvent : Id, IEvent, ICommand // Doesn't have ICommand so can't find handler
        {
        }

        public class Aggregate : AggregateBase,
            IHandleAggregateCommand<Create, Validate>,
            IHandleAggregateCommand<SendInvite, Validate>,
            IHandleAggregateCommand<Complain>,
            IHandleStatelessEvent<BadStatelessEvent>,
            IHandleStatelessEvent<EmailSent>,
            IHandleStatelessEvent<Refunded>
        {
            public void Handle(Create command, Validate validate)
            {
                var commandValidationResult = validate.Dispatch(command.Details);
                if (!commandValidationResult.IsValid)
                {
                    throw new DomainAggregateException(this, $"Validation Failed - {commandValidationResult.Errors}");
                }
                Raise<Created>(command);
            }

            public void Handle(SendInvite command, Validate validate)
            {
                var commandValidationResult = validate.Dispatch(command.Details);
                if (!commandValidationResult.IsValid)
                {
                    throw new DomainAggregateException(this, $"Validation Failed - {commandValidationResult.Errors}");
                }
                Raise<Created>(command);
                RaiseStatelessEvent(new Email.PersonInvited(){StreamId = command.CustomerId, EmailAddress = command.Email});
            }

            public void Handle(Complain command)
            {
                Raise<Complaint>(command);
                RaiseStatelessEvent(new NoteAdded(){CustomerId = command.CustomerId, Text = command.Reason});
            }

            //---------------------------------------------------------------------
            // ReSharper disable UnusedMember.Local
            private void Apply(Created ev)
            {
                if (ev.CustomerId == Guid.Empty)
                {
                    ev = null; // force exception below
                }
                Id = ev.CustomerId;
            }

            private void Apply(Complaint ev)
            {
                
            }

        }

        public class Validate : INeedExternalValidation<PersonalDetails>
        {
            public CommandValidationResult Dispatch(PersonalDetails details)
            {
                return string.IsNullOrWhiteSpace(details.Name) ? new CommandValidationResult(false, "Name can't be blank") : new CommandValidationResult(true);
            }
        }
    }
}
