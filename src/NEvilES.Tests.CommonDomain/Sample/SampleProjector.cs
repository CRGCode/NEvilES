using System;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;
using NEvilES.Tests.CommonDomain.Sample.ReadModel;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class SampleProjector :
        IProjectWithResult<Person.Created>,
        IProjectWithResult<Customer.Created>,
        IProjectWithResult<Person.NameCorrectedV2>
    {
        private readonly IWriteReadModel<Guid> writer;
        private readonly IReadFromReadModel<Guid> reader;

        public SampleProjector(IWriteReadModel<Guid> writer, IReadFromReadModel<Guid> reader)
        {
            this.writer = writer;
            this.reader = reader;
        }

        public IProjectorResult Project(Customer.Created message, IProjectorData data)
        {
            var person = new PersonReadModel(message.CustomerId, message.Details.FirstName, message.Details.LastName);
            writer.Insert(person);
            return new ProjectorResult(person);
        }

        public IProjectorResult Project(Person.NameCorrectedV2 message, IProjectorData data)
        {
            var person = reader.Get<PersonReadModel>(message.PersonId);
            person.FirstName = message.FirstName;
            person.LastName = message.LastName;
            writer.Update(person);

            return new ProjectorResult(person);
        }

        public IProjectorResult Project(Person.Created message, IProjectorData data)
        {       
            var person = new PersonReadModel(message.PersonId, message.Person.FirstName, message.Person.LastName);
            writer.Insert(person);
            return new ProjectorResult(person);
        }
    }

    public class SampleProjector2 :
        IProject<Person.Created>,
        IProject<Employee.BonusPaid>,
        IProject<Customer.EmailSent>
    {
        public void Project(Person.Created message, IProjectorData data)
        {
            // do something....
        }

        public void Project(Employee.BonusPaid message, IProjectorData data)
        {
            data.CommandContext.Result.ReadModelItems.Add(message.Amount);
        }

        public void Project(Customer.EmailSent message, IProjectorData data)
        {
            data.CommandContext.Result.ReadModelItems.Add(message.Text);
        }
    }
}
