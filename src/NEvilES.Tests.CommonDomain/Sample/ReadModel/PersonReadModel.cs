using System;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample.ReadModel
{
    public class PersonReadModel : PersonalDetails, IHaveIdentity<Guid>
    {
        public PersonReadModel(Guid id, string firstName, string lastName) : base(firstName, lastName)
        {
            Id = id;
        }

        public Guid Id { get; }
    }
}