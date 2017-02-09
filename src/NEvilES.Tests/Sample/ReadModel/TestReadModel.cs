using System;
using System.Collections.Generic;

namespace NEvilES.Tests.Sample.ReadModel
{
    public class TestReadModel : IReadModel
    {
        public TestReadModel()
        {
            People = new Dictionary<Guid, PersonalDetails>();
        }

        public Dictionary<Guid, PersonalDetails> People { get; }
    }
}