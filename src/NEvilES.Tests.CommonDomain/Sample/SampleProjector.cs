using System;
using System.Collections.Generic;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;
using NEvilES.Tests.CommonDomain.Sample.ReadModel;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class SampleProjector :
        IProjectWithResult<Person.Created>,
        IProjectWithResult<Person.NameCorrectedV2>
    {
        private readonly DocumentStoreGuid db;

        public SampleProjector(DocumentStoreGuid db)
        {
            this.db = db;
        }

        public IProjectorResult Project(Person.Created message, IProjectorData data)
        {
            var person = new PersonReadModel(message.PersonId, message.Person.FirstName, message.Person.LastName);
            db.Insert(person);
            return new ProjectorResult(person);
        }

        public IProjectorResult Project(Person.NameCorrectedV2 message, IProjectorData data)
        {
            var person = db.Get<PersonReadModel>(message.PersonId);
            person.FirstName = message.FirstName;
            person.LastName = message.LastName;
            db.Update(person);

            return new ProjectorResult(person);
        }
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
