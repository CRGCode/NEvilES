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
            public string Name { get; set; }
        }

        public class Created : Create, IEvent { }

        public class Complain : Id, ICommand
        {
            public string Reason { get; set; }
        }

        public class Complaint : Complain, IEvent { }

        public class Refunded : Id, IEvent
        {
            public decimal Amount { get; set; }

            public Refunded(Guid customerId, decimal amount)
            {
                CustomerId = customerId;
                Amount = amount;
            }

            public Refunded() { }
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
            IHandleAggregateCommand<Complain>,
            IHandleStatelessEvent<BadStatelessEvent>,
            IHandleStatelessEvent<EmailSent>,
            IHandleStatelessEvent<Refunded>
        {
            public void Handle(Create command, Validate validate)
            {
                var commandValidationResult = validate.Dispatch(command);
                if (!commandValidationResult.IsValid)
                {
                    throw new DomainAggregateException(this, $"Validation Failed - {commandValidationResult.Errors}");
                }
                Raise<Created>(command);
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
                Id = ev.CustomerId;
            }

            private void Apply(Complaint ev)
            {
                
            }
        }

        public class Validate : INeedExternalValidation<Create>
        {
            public CommandValidationResult Dispatch(Create command)
            {
                return string.IsNullOrWhiteSpace(command.Name) ? new CommandValidationResult(false, "Name can't be blank") : new CommandValidationResult(true);
            }
        }
    }
}
