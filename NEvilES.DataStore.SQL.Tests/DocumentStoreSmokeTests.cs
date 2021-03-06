using System;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions.Pipeline;
using Xunit;

namespace NEvilES.DataStore.SQL.Tests
{
    public class DocumentStoreSmokeTests : IClassFixture<TestContext>
    {
        private IReadFromReadModel reader;
        private IWriteReadModel writer;

        public DocumentStoreSmokeTests(TestContext context)
        {
            writer = context.Services.GetService<IWriteReadModel>();
            reader = context.Services.GetService<IReadFromReadModel>();
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

        [Fact]
        public void Save()
        {
            var id = Guid.NewGuid();
            var item = new Person(id, "John");
            writer.Save(item);

            var person = reader.Get<Person>(id);

            Assert.Equal("John", person.Name);
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
