using System;
using System.Linq;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class Person
    {
        public abstract class Id 
        {
            public Guid GetStreamId() => PersonId;
            public Guid PersonId { get; set; }
        }

        public class Create : Id, ICommand
        {
            public PersonalDetails Person { get; set; }
            public string ExtraEventInfo { get; set; }
        }

        public class Created : Create, IEvent
        {
            public Created(Guid id, PersonalDetails person)
            {
                PersonId = id;
                Person = person;
            }
        }

        public class SendInvite : Id, ICommand
        {
            public SendInvite(Guid id, PersonalDetails person, string email)
            {
                PersonId = id;
                Person = person;
                Email = email;
            }

            public PersonalDetails Person { get; set; }
            public string Email { get; set; }
        }


        public class StatelessBirthdateChanged : Id, IEvent
        {
            public DateTime Birthdate { get; set; }
        }

        public class CorrectName : Id, ICommand
        {
            public string Name { get; set; }
            public string Reason { get; set; }
        }

        public class NameCorrected : CorrectName, IEvent { }

        public class NameCorrectedV2 : Id, IEvent
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class AddComment : Id, ICommand
        {
            public string Comment { get; set; }
        }

        public class CommentAdded : AddComment, IEvent
        {
        }

        public class Aggregate : AggregateBase,
            IHandleStatelessEvent<StatelessBirthdateChanged>,
            IHandleAggregateCommand<CorrectName>,
            IHandleAggregateCommand<AddComment>
        {
            public string Name { get; private set; }

            protected void Handle(Create c)
            {
                GuardFromEmptyOrNulls(c.Person.FirstName, c.Person.FirstName);

                if (c.Person.LastName.Equals("God") && c.ExtraEventInfo != "")
                {
                    throw new DomainAggregateException(this, "No thanks, no God's allowed!");
                }

                RaiseEvent(new Created(c.PersonId, c.Person));
            }

            public void Handle(CorrectName c)
            {
                GuardFromEmptyOrNulls(c.Name);
                if (c.Name.Equals("God"))
                {
                    throw new DomainAggregateException(this, "No thanks, no God's allowed!");
                }

                const int limit = 20;
                if (nameCorrected > limit)
                {
                    throw new DomainAggregateException(this,
                        "Come on, you need to learn to type! Name correction limit of {0} exceeded", limit);
                }

                var e = new NameCorrectedV2
                {
                    PersonId = c.PersonId,
                    FirstName = c.Name.Split(' ').First(),
                    LastName = c.Name.Split(' ').Last()
                };

                RaiseEvent(e);
            }

            public void Handle(AddComment command)
            {
                RaiseStateless<CommentAdded>(command);
            }
            
            //---------------------------------------------------------------------
            // ReSharper disable UnusedMember.Local
            private void Apply(Created ev)
            {
                Id = ev.PersonId;
                Name = ev.Person.Name;
            }

            private int nameCorrected;

            private void Apply(NameCorrected ev)
            {
                Name = ev.Name;
                nameCorrected++;
            }

            private void Apply(NameCorrectedV2 ev)
            {
                Name = ev.FirstName + ' ' + ev.LastName;
                nameCorrected++;
            }
        }

        public class Validate : INeedExternalValidation<Create>
        {
            public CommandValidationResult Dispatch(Create command)
            {
                return new CommandValidationResult(true);
            }
        }
    }
}