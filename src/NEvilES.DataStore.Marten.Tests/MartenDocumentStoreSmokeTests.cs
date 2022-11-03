using System;
using System.Linq;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Testing;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.DataStore.Marten.Tests
{
    [Collection("Integration")]
    public class MartenDocumentStoreSmokeTests : IClassFixture<TestContext>
    {
        private readonly TestContext context;
        private readonly ITestOutputHelper output;

        public MartenDocumentStoreSmokeTests(TestContext context, ITestOutputHelper output)
        {
            this.context = context;
            this.output = output;
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
            writer.Insert(new Person(Guid.NewGuid(), "Fred"));

            var reader = context.Services.GetRequiredService<IReadFromReadModel<Guid>>();
            var persons = reader.Query<Person>(p => p.Name == "John").ToArray();

            Assert.Single(persons);
            Assert.Equal(item.Name, persons.First().Name);
        }

        [Fact]
        public void GetAll_Filter()
        {
            var id = Guid.NewGuid();
            var item = new Person(id, "John") { State = State.Vic };
            var writer = context.Services.GetRequiredService<IWriteReadModel<Guid>>();
            writer.Insert(item);
            writer.Insert(new Person(Guid.NewGuid(), "Fred"));


            var reader = context.Services.GetRequiredService<IQueryFromReadModel<Guid>>();
            var query = reader
                .GetAll<Person>();

            query = query.FilterByName("John");
            query = query.FilterByState(State.Vic);
            //query = query.FilterByStates(new []{ State.Vic, State.NSW });

            query = query.OrderByDescending(x => x.DOB);
            output.WriteLine(query.ToCommand().CommandText);
            Assert.Contains("->> 'Name'", query.ToCommand().CommandText);
            //Assert.Contains("->> 'State'", query.ToCommand().CommandText);
            Assert.Contains("order by", query.ToCommand().CommandText);

            var results = query.ToArray();
            Assert.Single(results);
        }
    }

    public static class PersonFilters
    {
        public static IQueryable<Person> FilterByName(this IQueryable<Person> query, string name)
        {
            return string.IsNullOrWhiteSpace(name)  ? query : query.Where(x => x.Name == name);
        }
        public static IQueryable<Person> FilterByStates(this IQueryable<Person> query, State[] states)
        {
            return query.Where(person => states.Any(s => person.State == s));
        }
        public static IQueryable<Person> FilterByState(this IQueryable<Person> query, State state)
        {
            return query.Where(person => person.State == state);
        }

    }

    public class Person : IHaveIdentity<Guid>
    {
        public Guid Id { get; }
        public string Name { get; set; }
        public DateTime DOB { get; set; }
        public State State { get; set; }

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