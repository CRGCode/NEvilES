using System;
using System.Collections.Generic;
using NEvilES.Pipeline;

namespace NEvilES.Tests.Sample
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

        public ProjectorResult Project(Person.Created message, ProjectorData data)
        {
            db.People.Add(message.StreamId, message.Person);
            return new ProjectorResult(message.Person);
        }

        public ProjectorResult Project(Person.NameCorrectedV2 message, ProjectorData data)
        {
            var person = db.People[message.StreamId];
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
        IProject<Customer.SendEmail>
    {
        public void Project(Person.Created message, ProjectorData data)
        {
            // do something....
        }

        public void Project(Employee.PaidBonus message, ProjectorData data)
        {
            data.CommandContext.Result.ReadModelItems.Add(message.Amount);
        }

        public void Project(Customer.SendEmail message, ProjectorData data)
        {
            data.CommandContext.Result.ReadModelItems.Add(message.Text);
        }
    }
}
