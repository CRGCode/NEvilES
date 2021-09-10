using System;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class Customer
    {
        public class Create : ICommand
        {
            public Guid StreamId { get; set; }
            public string Name { get; set; }
        }

        public class Created : Create, IEvent { }

        public class Complain : Complaint, ICommand { }

        public class Complaint : Event 
        {
            public string Reason { get; set; }
        }

        public class Refunded : IEvent
        {
            public Guid StreamId { get; set; }
            public decimal Amount { get; set; }

            public Refunded(Guid streamId, decimal amount)
            {
                StreamId = streamId;
                Amount = amount;
            }

            public Refunded() { }
        }

        public class EmailSent : IEvent
        {
            public Guid StreamId { get; set; }
            public string Text { get; set; }
        }

        public class NoteAdded : Event
        {
            public string Text { get; set; }
        }

        public class BadStatelessEvent : IEvent, ICommand // Doesn't have ICommand so can't find handler
        {
            public Guid StreamId { get; set; }
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
                RaiseStatelessEvent(new NoteAdded(){StreamId = command.StreamId, Text = command.Reason});
            }

            //---------------------------------------------------------------------
            // ReSharper disable UnusedMember.Local
            private void Apply(Created ev)
            {
                Id = ev.StreamId;
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
