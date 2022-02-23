using System;
using System.Linq;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Tests.CommonDomain.Sample.ReadModel;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class UniqueNameValidation :
        INeedExternalValidation<Employee.Create>
    {
        private readonly IReadFromReadModel<Guid> db;

        public UniqueNameValidation(IReadFromReadModel<Guid> db)
        {
            this.db = db;
        }

        public CommandValidationResult Dispatch(Employee.Create command)
        {
            var result = !db.Query<PersonReadModel>(x => x.Name == $"{command.Person.FirstName} {command.Person.LastName}").Any();
            return new CommandValidationResult(result, $"Person already exists - {command.Person.FirstName} {command.Person.LastName}");
        }
    }
}