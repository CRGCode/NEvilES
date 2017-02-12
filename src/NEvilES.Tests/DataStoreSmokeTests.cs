using System;
using System.Data;
using System.Diagnostics;
using NEvilES.DataStore;
using NEvilES.Pipeline;
using NEvilES.Tests.Sample;
using StructureMap;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.Tests
{
    public class DataStoreSmokeTests : IClassFixture<SharedFixtureContext>, IDisposable
    {
        //This is use instead of Console.Write or Debug.Write
        private readonly ITestOutputHelper output;
        private IRepository repository;
        private IContainer container;
        private IDbTransaction transaction;

        public DataStoreSmokeTests(ITestOutputHelper helper, SharedFixtureContext context)
        {
            output = helper;
            container = context.Container.GetNestedContainer();
            context.Container.Configure(x =>
            {
                x.For<IConnectionString>().Use(s => new SqlConnectionString("Server=(localdb)\\MSSQLLocalDB;Database=es_test;Integrated Security=true;"));
            });

            var conn = container.GetInstance<IDbConnection>();
            conn.Open();
            transaction = conn.BeginTransaction(IsolationLevel.ReadUncommitted);
            repository = new DatabaseEventStore(transaction, new EventTypeLookupStrategy(),
                new CommandContext(new CommandContext.User(Guid.NewGuid(), 666), Guid.NewGuid(), Guid.NewGuid(), new CommandContext.User(Guid.NewGuid(), 007), ""));
        }

        [Fact]
        public void Get_NoEvents()
        {
            var streamId = Guid.NewGuid();

            var expected = repository.Get<Customer.Aggregate>(streamId);
            Assert.NotNull(expected);
            Assert.Equal(expected.Id, streamId);
            Assert.Equal(expected.Version, 0);
        }

        [Fact]
        public void Save_Events()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Handle(new Customer.Create { StreamId = streamId, Person = new PersonalDetails("John", "Citizen") });

            var expected = repository.Save(agg);
            Assert.NotNull(expected);
            Assert.Equal(expected.StreamId, streamId);
            Assert.Equal(expected.UpdatedEvents.Length, 1);
        }

         [Fact]
        public void Save_Events_Stateless()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Handle(new Customer.Create { StreamId = streamId, Person = new PersonalDetails("John", "Citizen") });
            agg.RaiseStatelessEvent(new Customer.Refunded(streamId, 800.80M));

            var expected = repository.Save(agg);
            Assert.NotNull(expected);
            Assert.Equal(expected.StreamId, streamId);
            Assert.Equal(expected.UpdatedEvents.Length, 2);
        }

        void IDisposable.Dispose()
        {
            //Use this to commit events testdata
            //transaction.Commit();
        }

        //[Fact]
        //public void CommandWithDifferentEventHandlerOnAggregateWithException()
        //{
        //    var streamId = Guid.NewGuid();
        //    Assert.Throws<DomainAggregateException>(() =>
        //        _commandProcessor.Process(new Employee.Create
        //        {
        //            StreamId = streamId,
        //            Person = new PersonalDetails("John", "God")
        //        }));
        //}
    }
}