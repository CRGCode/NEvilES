using System;
using System.Collections.Generic;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace GTD.Domain
{
    public abstract class Request
    {
        public abstract class Id : IMessage
        {
            public Guid RequestId { get; set; }
            public Guid GetStreamId() => RequestId;
        }

        public class NewRequest : Id, ICommand
        {
            public Guid ProjectId { get; set; }
            public string ShortName { get; set; }
            public string Description { get; set; }
            public int Priority { get; set; }

            public List<string> AttachedFiles { get; set; }
        }

        public class Created : NewRequest, IEvent { }

        public class AddComment : Id, ICommand
        {
            public string Text { get; set; }
        }
        public class CommentAdded : AddComment, IEvent { }

        public class Accept : Id, ICommand
        {
            public DateTimeOffset CompleteDate { get; set; }
        }

        public class Accepted : Accept, IMapEvent<Accepted, Accept>
        {
            public Accepted Map(Accept c)
            {
                return new Accepted
                {
                    CompleteDate = c.CompleteDate
                };
            }
        }

        public class Cancel : Id, ICommand
        {
            public string Reason { get; set; }
        }
        public class Cancelled : Cancel, IEvent { }

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
                Raise<Accepted, Accept>(command);
            }

            public void Handle(Cancel command)
            {
                if (state == RequestState.Cancelled)
                    throw new DomainAggregateException(this, "Request already cancelled");
                Raise<Cancelled>(command);
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