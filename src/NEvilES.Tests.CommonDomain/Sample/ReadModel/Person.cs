using System;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample.ReadModel
{
    public class Person : IHaveIdentity<Guid>
    {
        public Guid Id { get; set; }
    }
}