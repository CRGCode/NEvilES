using System;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions.Pipeline;
using Xunit;

namespace NEvilES.DataStore.SQL.Tests
{
    //[CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
    [Collection("Serial")]
    public class DocumentStoreSmokeTests : IClassFixture<SQLTestContext>
    {
        private readonly IReadFromReadModel<Guid> reader;
        private readonly IWriteReadModel<Guid> writer;

        public DocumentStoreSmokeTests(SQLTestContext context)
        {
            writer = context.Container.GetService<IWriteReadModel<Guid>>();
            reader = context.Container.GetService<IReadFromReadModel<Guid>>();
        }

        [Fact]
        public void Insert()
        {
            var id = Guid.NewGuid();
            writer.Insert(new Person(id, "Craig"));

            var person = reader.Get<Person>(id);

            Assert.Equal("Craig", person.Name);
        }

        [Fact]
        public void Update()
        {
            var id = Guid.NewGuid();
            var item = new Person(id, "Craig");
            writer.Insert(item);
            item.Name = "Fred";
            writer.Update(item);

            var person = reader.Get<Person>(id);

            Assert.Equal("Fred", person.Name);
        }
    }

    public class Person : IHaveIdentity<Guid>
    {
        public Guid Id { get; }
        public string Name { get; set; }

        public Person(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
