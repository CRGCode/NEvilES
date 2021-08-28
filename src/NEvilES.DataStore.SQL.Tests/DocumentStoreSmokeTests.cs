using System;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions.Pipeline;
using Xunit;

namespace NEvilES.DataStore.SQL.Tests
{
    public class DocumentStoreSmokeTests : IClassFixture<SQLTestContext>
    {
        private IReadFromReadModel reader;
        private IWriteReadModel writer;

        public DocumentStoreSmokeTests(SQLTestContext context)
        {
            writer = context.Container.GetService<IWriteReadModel>();
            reader = context.Container.GetService<IReadFromReadModel>();
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

    public class Person : IHaveIdentity
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
