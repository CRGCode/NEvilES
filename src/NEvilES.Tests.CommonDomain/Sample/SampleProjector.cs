using System;
using System.Collections.Generic;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class SampleProjector :
        IProjectWithResult<Person.Created>,
        IProjectWithResult<Person.NameCorrectedV2>
    {
        private readonly IReadModel db;

        public SampleProjector(IReadModel db)
        {
            this.db = db;
        }

        public IProjectorResult Project(Person.Created message, IProjectorData data)
        {
            db.People.Add(message.PersonId, message.Person);
            return new ProjectorResult(message.Person);
        }

        public IProjectorResult Project(Person.NameCorrectedV2 message, IProjectorData data)
        {
            var person = db.People[message.PersonId];
            person.FirstName = message.FirstName;
            person.LastName = message.LastName;

            return new ProjectorResult(person);
        }
    }

    public interface IReadModel
    {
        Dictionary<Guid, PersonalDetails> People { get; }
    }

    public class SampleProjector2 :
        IProject<Person.Created>,
        IProject<Employee.PaidBonus>,
        IProject<Customer.EmailSent>
    {
        public void Project(Person.Created message, IProjectorData data)
        {
            // do something....
        }

        public void Project(Employee.PaidBonus message, IProjectorData data)
        {
            data.CommandContext.Result.ReadModelItems.Add(message.Amount);
        }

        public void Project(Customer.EmailSent message, IProjectorData data)
        {
            data.CommandContext.Result.ReadModelItems.Add(message.Text);
        }
    }
}
