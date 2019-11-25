using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using NEvilES.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NEvilES.Tests
{
    public class SnapshotTests
    {
        //     public class SnapshotDynamoDBEventStore : DynamoDBEventStore, ISnapshotRepository
        //     {
        //         private DynamoDBContext _context;
        //         private IEventTypeLookupStrategy _eventTypeLookupStrategy;

        //         public SnapshotDynamoDBEventStore(
        //             IAmazonDynamoDB dynamoDbClient,
        //             IEventTypeLookupStrategy eventTypeLookupStrategy,
        //             ICommandContext commandContext)
        //         : base(dynamoDbClient, eventTypeLookupStrategy, commandContext)
        //         {
        //             _context = new DynamoDBContext(dynamoDbClient);
        //             _eventTypeLookupStrategy = eventTypeLookupStrategy;
        //         }

        //         public async override Task<IAggregate> GetAsync(Type type, Guid id)
        //         {
        //             // does aggregate type inherit ISnapshotAggregate
        //             IAggregate aggregate = null;

        //             if (type.GetInterfaces().Contains(typeof(ISnapshotAggregate<ISnapshotState>)))
        //             {
        //                 var snapshot = await GetSnapshotAsync(type, id);
        //                 if (snapshot != null)
        //                 {
        //                     ((ISnapshotAggregate<ISnapshotState>)(aggregate)).ApplySnapshot(snapshot);

        //                     var remainingEvents = await GetRemainingEventsAsync(type, id, snapshot.Version);

        //                     if (remainingEvents.Count > 0)
        //                     {
        //                         foreach (var eventDb in remainingEvents.OrderBy(x => x.Version))
        //                         {
        //                             var message =
        //                                 (IEvent)
        //                                 JsonConvert.DeserializeObject(eventDb.Body, _eventTypeLookupStrategy.Resolve(eventDb.BodyType), SerializerSettings);
        //                             message.StreamId = eventDb.StreamId;
        //                             aggregate.ApplyEvent(message);
        //                         }
        //                     }
        //                     return aggregate;
        //                 }
        //             }

        //             return await base.GetAsync(type, id);
        //         }

        //         private async Task<ISnapshot<ISnapshotState>> GetSnapshotAsync(Type type, Guid id)
        //         {



        //             return new Snapshot<ISnapshotState>()
        //             {

        //             };
        //         }

        //         private async Task<List<DynamoDBEvent>> GetRemainingEventsAsync(Type type, Guid id, Int64 version)
        //         {
        //             var expression = new Expression()
        //             {
        //                 ExpressionStatement = $"{nameof(DynamoDBEvent.StreamId)} = :sId AND {nameof(DynamoDBEvent.Version)} > :v",
        //                 ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>{
        //                     {":sId",id.ToString()},
        //                     {":v",  version}
        //                 }
        //             };

        //             var query = _context.FromQueryAsync<DynamoDBEvent>(new QueryOperationConfig()
        //             {
        //                 ConsistentRead = true,
        //                 KeyExpression = expression
        //             });

        //             var events = await query.GetRemainingAsync();

        //             return events;
        //         }
        //     }


        //     // public async override Task<IAggregateCommit> SaveAsync(IAggregate aggregate)
        //     // {

        //     // }
        // }

        public class TestState : ISnapshotState { }


        public interface ISnapshotRepository
        {

        }

        public abstract class SnapshotAggregateBase<TStateModel> : AggregateBase, ISnapshotAggregate<TStateModel> where TStateModel : class, ISnapshotState
        {
            public TStateModel State { get; protected set; }

            public void ApplySnapshot(ISnapshot<TStateModel> snapshot)
            {
                this.SetState(snapshot.Id, snapshot.Version);
                this.State = snapshot.State;
            }

            public ISnapshot<TStateModel> GetSnapshot()
            {
                return new Snapshot<TStateModel>
                {
                    Id = this.Id,
                    State = this.State,
                    Version = this.Version,
                    When = DateTimeOffset.Now
                };
            }
        }

        public interface ISnapshotAggregate<TStateModel> where TStateModel : class, ISnapshotState
        {
            TStateModel State { get; }
            ISnapshot<TStateModel> GetSnapshot();
            void ApplySnapshot(ISnapshot<TStateModel> snapshot);
        }

        public interface ISnapshotState { }
        public interface ISnapshot<TState> where TState : class, ISnapshotState
        {
            Guid Id { get; set; }
            int Version { get; set; }
            TState State { get; set; }
            DateTimeOffset When { get; set; }
        }

        public interface ISnapshotTable
        {
            Guid Id { get; set; }
            int Version { get; set; }
            string State { get; set; }
            Type StateType { get; set; }
            DateTimeOffset When { get; set; }
        }

        public class Snapshot<TState> : ISnapshot<TState> where TState : class, ISnapshotState
        {
            public Guid Id { get; set; }
            public int Version { get; set; }
            public TState State { get; set; }
            public DateTimeOffset When { get; set; }
        }

        public class MyContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .Select(p => base.CreateProperty(p, memberSerialization))
                            .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                       .Select(f => base.CreateProperty(f, memberSerialization)))
                            .ToList();
                props.ForEach(p => { p.Writable = true; p.Readable = true; });
                return props;
            }
        }
    }
}