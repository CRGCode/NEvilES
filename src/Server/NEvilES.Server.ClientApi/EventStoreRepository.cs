//using System;
//using System.Linq;
//using System.Text;
//using CRG.ES.Messages;
//using NEvilES.Server.Abstractions;
//using Newtonsoft.Json;

//namespace CRG.ES.ClientAPI
//{
//	public class EventStoreRepository : IRepository
//	{
//		private readonly IEventStore eventStore;
//		private readonly ProcessDescriptor processDescriptor;

//		public EventStoreRepository(IEventStore eventStore, ProcessDescriptor processDescriptor)
//		{
//			this.eventStore = eventStore;
//			this.processDescriptor = processDescriptor;
//		}

//		public TAggregate GetById<TAggregate>(Guid id) where TAggregate : class, IAggregate
//		{
//		    var stream = eventStore.ReadStream(id);
//			var aggregate = (TAggregate) Activator.CreateInstance(typeof (TAggregate), true);

//			foreach (var e in stream.Events.OrderBy(x=>x.Version))
//			{
//				var type = Type.GetType(e.Type);
//				var @event = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(e.Data), type);
//				try
//				{
//					aggregate.ApplyEvent(@event);
//				}
//				catch (Exception)
//				{
//					// Should never happen but if the programmer get it's wrong then let's help him with more info :)
//					throw new Exception($"Failed to ApplyEvent {type.Name} on Aggregate {aggregate.GetType().Name}");
//				}
//			}

//			return aggregate;
//		}

//		public AggregateCommit Save(IAggregate aggregate)
//		{
//			var uncommittedEvents = aggregate.GetUncommittedEvents().Cast<IEventData>().ToArray();
//			var commit = new StoreEventsRequest
//				{
//					StreamId = aggregate.Id,
//					CurrentVersion = aggregate.Version,
//					Timestamp = DateTimeOffset.Now,
//					Username = null,
//					Source = processDescriptor.Name,
//					Events = uncommittedEvents
//									  .Select(o => new Tuple<string, object>(o.GetType().AssemblyQualifiedName, o))
//									  .ToList()
//				};

//			eventStore.WriteStream(commit);
//			return new AggregateCommit(aggregate.Id, uncommittedEvents);
//		}
//	}
//}