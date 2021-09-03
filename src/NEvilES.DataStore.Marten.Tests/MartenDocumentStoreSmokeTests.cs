using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using Xunit;

namespace NEvilES.DataStore.Marten.Tests
{
    public class MartenDocumentStoreSmokeTests : IClassFixture<TestContext>
    {
        private readonly TestContext context;

        public MartenDocumentStoreSmokeTests(TestContext context)
        {
            this.context = context;
        }

        [Fact]
        public void Insert()
        {
            var writer = context.Services.GetRequiredService<IWriteReadModel<Guid>>();
            var id = Guid.NewGuid();
            writer.Insert(new Person(id, "Craig"));

            var reader = context.Services.GetRequiredService<IReadFromReadModel<Guid>>();
            var person = reader.Get<Person>(id);

            Assert.Equal("Craig", person.Name);
        }

        [Fact]
        public void Update()
        {
            var id = Guid.NewGuid();
            var item = new Person(id, "Craig");
            var writer = context.Services.GetRequiredService<IWriteReadModel<Guid>>();

            writer.Insert(item);
            item.Name = "Fred";
            writer.Update(item);

            var reader = context.Services.GetRequiredService<IReadFromReadModel<Guid>>();
            var person = reader.Get<Person>(id);

            Assert.Equal("Fred", person.Name);
        }

        [Fact]
        public void Query()
        {
            var id = Guid.NewGuid();
            var item = new Person(id, "John");
            var writer = context.Services.GetRequiredService<IWriteReadModel<Guid>>();
            writer.Insert(item);

            var reader = context.Services.GetRequiredService<IReadFromReadModel<Guid>>();
            var person = reader.Query<Person>(p => p.Name == "John").First();

            Assert.Equal("John", person.Name);
        }

        [Fact]
        public void EventStoreCreate()
        {
            new PgSQLEventStoreCreate().CreateOrWipeDb(new ConnectionString("Host=localhost;Username=postgres;Password=password;Database=originations"));
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