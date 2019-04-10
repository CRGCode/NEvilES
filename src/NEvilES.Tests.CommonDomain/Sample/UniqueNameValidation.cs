using System.Linq;

namespace NEvilES.Tests.CommonDomain.Sample
{
    public class UniqueNameValidation :
        INeedExternalValidation<Person.Create>
    {
        private readonly IReadModel db;

        public UniqueNameValidation(IReadModel db)
        {
            this.db = db;
        }

        public CommandValidationResult Dispatch(Person.Create command)
        {
            var result = db.People.Values.All(x => x.Name != $"{command.Person.FirstName} {command.Person.LastName}");
            return new CommandValidationResult(result, $"Person already exists - {command.Person.FirstName} {command.Person.LastName}");
        }
    }
}