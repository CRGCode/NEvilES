using System.Linq;

namespace NEvilES.Tests.Sample
{
    public class UniqueNameValidation :
        INeedExternalValidation<Person.Create>
    {
        private readonly IReadModel _db;

        public UniqueNameValidation(IReadModel db)
        {
            _db = db;
        }

        public CommandValidationResult Dispatch(Person.Create command)
        {
            var result = _db.People.Values.All(x => x.Name != $"{command.Person.FirstName} {command.Person.LastName}");
            return new CommandValidationResult(result, $"Person already exists - {command.Person.FirstName} {command.Person.LastName}");
        }
    }
}