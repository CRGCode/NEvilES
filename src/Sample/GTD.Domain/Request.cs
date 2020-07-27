using System;
using System.Collections.Generic;
using NEvilES;
using NEvilES.Abstractions;

namespace GTD.Domain
{
    public abstract class Request
    {
        public class NewRequest : ICommand
        {
            public Guid StreamId { get; set; }
            public Guid ProjectId { get; set; }
            public string ShortName { get; set; }
            public string Description { get; set; }
            public int Priority { get; set; }

            public List<string> AttachedFiles { get; set; }
        }
        public class Created : NewRequest, IEvent
        {
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
            IHandleStatelessEvent<CommentAdded>,
            IHandleAggregateCommand<Accept>,
            IHandleAggregateCommand<Cancel>
        {
            public ICommandResponse Handle(NewRequest command, UniqueValidator uniqueValidator)
            {
                if (uniqueValidator.Dispatch(command).IsValid)
                {
                    Raise<Created>(command);
                    return new CommandCompleted(command.StreamId, nameof(NewRequest));
                }

                return new CommandRejectedWithError<string>(command.StreamId, nameof(NewRequest), "Can't accept a cancelled request");
            }

            public ICommandResponse Handle(Accept command)
            {
                if (state == RequestState.Cancelled)
                    return new CommandRejectedWithError<string>(command.StreamId, nameof(Accept), "Can't accept a cancelled request");
                // throw new DomainAggregateException(this, "Can't accept a cancelled request");
                RaiseEvent<Accepted>(command);
                return new CommandCompleted(command.StreamId, nameof(Accept));
            }

            public ICommandResponse Handle(Cancel command)
            {
                if (state == RequestState.Cancelled)
                    return new CommandRejectedWithError<string>(command.StreamId, nameof(Cancel), "Request already cancelled");
                // throw new DomainAggregateException(this, "Request already cancelled");
                RaiseEvent<Cancelled>(command);

                return new CommandCompleted(command.StreamId, nameof(Cancel));
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