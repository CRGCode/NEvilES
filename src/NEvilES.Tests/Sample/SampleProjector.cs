using System;
using System.Collections.Generic;
using NEvilES.Pipeline;

namespace NEvilES.Tests.Sample
{
    public class SampleProjector : 
        IProjectWithResult<Person.Created>, 
        IProjectWithResult<Person.NameCorrectedV2>
    {
        private readonly IReadModel _db;

        public SampleProjector(IReadModel db)
        {
            _db = db;
        }

        public ProjectorResult Project(Person.Created message, ProjectorData data)
        {
            _db.People.Add(message.StreamId, message.Person);
            return new ProjectorResult(message.Person);
        }

        public ProjectorResult Project(Person.NameCorrectedV2 message, ProjectorData data)
        {
            var person = _db.People[message.StreamId];
            person.FirstName = message.FirstName;
            person.LastName = message.LastName;

            return new ProjectorResult(person);
        }
    }

    public interface IReadModel
    {
        Dictionary<Guid, PersonalDetails> People { get; }
    }

    public class SampleProjector2 : IProject<Person.Created>
    {
        public void Project(Person.Created message, ProjectorData data)
        {
            // do something....
        }
    }
}
