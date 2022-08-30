//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Newtonsoft.Json;

//namespace CRG.ES.ClientAPI
//{
//    public class EventStoreStreamReader : IAccessEventStore
//    {
//        private readonly IEventStore eventStore;

//        public EventStoreStreamReader(IEventStore eventStore)
//        {
//            this.eventStore = eventStore;
//        }

//        public IEnumerable<IEventData> Get(DateTime @from)
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<IEventData> Get(string streamId)
//        {
//            var stream = eventStore.ReadStream(Guid.Parse(streamId));

//            return stream.Events.Select(x =>
//            {
//                var type = Type.GetType(x.Type);
//                var @event = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(x.Data), type);
//                return new EventData(x.Type, @event, DateTime.Now, x.Version);
//            }).OrderBy(x => x.Version);
//        }
//    }
//}