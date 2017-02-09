using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using NEvilES.DataStore;
using NEvilES.Pipeline;
using NEvilES.Tests.Sample;
using StructureMap;
using Xunit;

namespace NEvilES.Tests
{
    public class DataStoreSmokeTests : IUseFixture<SharedFixtureContext>
    {
        private IRepository _repository;
        private IContainer _container;

        public void SetFixture(SharedFixtureContext context)
        {
            _container = context.Container.GetNestedContainer();
            context.Container.Configure(x =>
            {
                x.For<IConnectionString>().Use(s => new SQLConnectionString("Server=.;Database=atlas;User Id=atlas;Password=atlas;MultipleActiveResultSets=True"));
            });
            var conn = _container.GetInstance<IDbConnection>();
            conn.Open();
            var transaction = conn.BeginTransaction();
            _repository = new DatabaseEventStore(transaction, new EventTypeLookupStrategy(), 
                new CommandContext(new CommandContext.User(Guid.NewGuid(), 666), Guid.NewGuid(), Guid.NewGuid(), new CommandContext.User(Guid.NewGuid(), 007), ""));
        }

        [Fact]
        public void Get_NoEvents()
        {
            var streamId = Guid.NewGuid();

            var expected = _repository.Get<Customer.Aggregate>(streamId);
            Assert.NotNull(expected);
            Assert.Equal(expected.Id,streamId);
            Assert.Equal(expected.Version,0);
        }

        [Fact]
        public void Save_Events()
        {
            var streamId = Guid.NewGuid();
            var agg = new Customer.Aggregate();
            agg.Handle(new Customer.Create {StreamId = streamId, Person = new PersonalDetails("John","Citizen")});

            var expected = _repository.Save(agg);
            Assert.NotNull(expected);
            Assert.Equal(expected.StreamId, streamId);
            Assert.Equal(expected.UpdatedEvents.Length, 1);
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