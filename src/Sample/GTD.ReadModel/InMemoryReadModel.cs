using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GTD.Common;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;

namespace GTD.ReadModel
{
    public class InMemoryReadModel : IReadFromReadModel, IWriteReadModel
    {
        private readonly ConcurrentDictionary<Guid, object> data;

        public InMemoryReadModel()
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
            return (T)data[id];
        }

        public IEnumerable<T> Query<T>(Func<T,bool> p)
        {
            return data.Values.Where(x => x.GetType() == typeof(T)).Cast<T>().Where(p);
        }

        public void Clear()
        {
            data.Clear();
        }

        public int Count()
        {
            return data.Count;
        }

        void IWriteReadModel.Save<T>(T item)
        {
            throw new NotImplementedException();
        }

        void IWriteReadModel.Delete<T>(T item)
        {
            throw new NotImplementedException();
        }
    }
}