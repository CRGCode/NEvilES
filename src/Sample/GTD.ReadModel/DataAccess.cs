using System;
using System.Collections.Concurrent;
using GTD.Common;

namespace GTD.ReadModel
{
    public class DataAccess : IReadData, IWriteData
    {
        private readonly ConcurrentDictionary<Guid, object> data;

        public DataAccess()
        {
            data = new ConcurrentDictionary<Guid, object>();
        }

        public void Insert<T>(T item) where T : class, IHaveIdentity
        {
            data.TryAdd(item.Id, item);
        }

        public void Update<T>(T item) where T : class, IHaveIdentity
        {
            data[item.Id] = item;
        }

        public T Get<T>(Guid id) where T : IHaveIdentity
        {
            return (T) data[id];
        }
    }
}