using System;
using System.Collections.Generic;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace GTD.Domain
{
    public abstract class Request
    {
        public class NewRequest : Created, ICommand { }

        public class Created : Event
        {
            public Guid ProjectId { get; set; }
            public string ShortName { get; set; }
            public string Description { get; set; }
            public int Priority { get; set; }

            public List<string> AttachedFiles { get; set; }
        }

        public class AddComment : CommentAdded, ICommand { }
        public class CommentAdded : Event
        {
            public string Text { get; set; }
        }

        public class Accept : Accepted, ICommand { }
        public class Accepted : Event
        {
            public DateTimeOffset CompleteDate { get; set; }
        }

        public class Cancel : Cancelled, ICommand { }
        public class Cancelled : Event
        {
            public string Reason { get; set; }
        }

        public class Aggregate : AggregateBase,
            IHandleAggregateCommand<NewRequest, UniqueValidator>,
            IHandleAggregateCommand<AddComment>,
            IHandleAggregateCommand<Accept>,
            IHandleAggregateCommand<Cancel>
        {
            public void Handle(NewRequest command, UniqueValidator uniqueValidator)
            {
                if (uniqueValidator.Dispatch(command).IsValid)
                    Raise<Created>(command);
            }

            public void Handle(AddComment command)
            {
                if (state == RequestState.Cancelled)
                    throw new DomainAggregateException(this, "Can't accept a cancelled request");
                RaiseStateless<CommentAdded>(command);
            }

            public void Handle(Accept command)
            {
                if (state == RequestState.Cancelled)
                    throw new DomainAggregateException(this, "Can't accept a cancelled request");
                RaiseEvent<Accepted>(command);
            }

            public void Handle(Cancel command)
            {
                if (state == RequestState.Cancelled)
                    throw new DomainAggregateException(this, "Request already cancelled");
                RaiseEvent<Cancelled>(command);
            }

            //-------------------------------------------------------------------
            private RequestState state;

            private void Apply(Created e)
            {
                state = RequestState.Created;
            }

            private void Apply(Accepted e)
            {
                state = RequestState.Accepted;
            }

            private void Apply(Cancelled e)
            {
                state = RequestState.Cancelled;
            }
        }

        public class UniqueValidator :
            INeedExternalValidation<NewRequest>
        {
            public CommandValidationResult Dispatch(NewRequest command)
            {
                // Check ReadModel
                return new CommandValidationResult(true);
            }
        }
    }

    public enum RequestState
    {
        Created,
        Accepted,
        Cancelled
    }
}