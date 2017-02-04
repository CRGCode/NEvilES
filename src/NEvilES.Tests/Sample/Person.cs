using System;
using System.Linq;

namespace NEvilES.Tests.Sample
{
    public class Person
    {
        public abstract class Create : ICommand
        {
            public Guid StreamId { get; set; }
            public PersonalDetails Person { get; set; }
            public string ExtraEventInfo { get; set; }
        }

        public class SendInvite : ICommand
        {
            public SendInvite(Guid id, PersonalDetails person, string email)
            {
                StreamId = id;
                Person = person;
                Email = email;
            }

            public Guid StreamId { get; set; }
            public PersonalDetails Person { get; set; }
            public string Email { get; set; }
        }

        public class Created : IEvent
        {
            public Guid StreamId { get; set; }
            public PersonalDetails Person { get; set; }

            public Created(Guid id, PersonalDetails person)
            {
                StreamId = id;
                Person = person;
            }
        }

        public class StatelessBirthdateChanged : IEvent
        {
            public Guid StreamId { get; set; }
            public DateTime Birthdate { get; set; }
        }

        public class CorrectName : ICommand
        {
            public Guid StreamId { get; set; }
            public string Name { get; set; }
            public string Reason { get; set; }
        }

        public class NameCorrected : IEvent
        {
            public Guid StreamId { get; set; }
            public string Name { get; set; }
        }

        public class NameCorrectedV2 : IEvent
        {
            public Guid StreamId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public abstract class Aggregate : AggregateBase,
            IHandleStatelessEvent<StatelessBirthdateChanged>,
            IHandleAggregateCommand<CorrectName>
        {
            public string Name { get; private set; }

            protected void Handle(Create c)
            {
                GuardFromEmptyOrNulls(c.Person.FirstName, c.Person.FirstName);

                if (c.Person.LastName.Equals("God") && c.ExtraEventInfo != "")
                {
                    throw new DomainAggregateException(this, "No thanks, the God's allowed!");
                }

                RaiseEvent(new Created(c.StreamId, c.Person));
            }

            public void Handle(CorrectName c)
            {
                GuardFromEmptyOrNulls(c.Name);
                if (c.Name.Equals("God"))
                {
                    throw new DomainAggregateException(this, "No thanks, the God's allowed!");
                }

                const int limit = 20;
                if (nameCorrected > limit)
                {
                    throw new DomainAggregateException(this,
                        "Come on, you need to learn to type! Name correction limit of {0} exceeded", limit);
                }

                var e = new NameCorrectedV2()
                {
                    StreamId = c.StreamId,
                    FirstName = c.Name.Split(' ').First(),
                    LastName = c.Name.Split(' ').Last()
                };

                RaiseEvent(e);
            }

            //---------------------------------------------------------------------
            // ReSharper disable UnusedMember.Local
            private void Apply(Created ev)
            {
                Id = ev.StreamId;
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
    }
}