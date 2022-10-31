using System;
using System.Linq;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Testing;
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
        public void GetQuery()
        {
            var id = Guid.NewGuid();
            var item = new Person(id, "John");
            var writer = context.Services.GetRequiredService<IWriteReadModel<Guid>>();
            writer.Insert(item);

            var reader = context.Services.GetRequiredService<IReadFromReadModel<Guid>>();
            var person = ((DocumentRepositoryWithKeyTypeGuid) reader)
                .GetQuery<Person>(p => p.Name == "John")
                .OrderBy(x => x.Name);

            Assert.Contains("->> 'Name'", person.ToCommand().CommandText);
            Assert.Contains("order by", person.ToCommand().CommandText);
        }


        [RunnableInDebugOnly]
        public void EventStoreCreate()
        {
            new PgSQLEventStoreCreate().CreateOrWipeDb(new ConnectionString(TestContext.ConnString));
        }
    }

    public class Person : IHaveIdentity<Guid>
    {
        public Guid Id { get; }
        public string Name { get; set; }

        public Person()
        {
            Name = "Blank";
        }

        public Person(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}