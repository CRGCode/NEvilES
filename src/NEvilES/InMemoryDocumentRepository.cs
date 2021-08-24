using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES
{
    public class InMemoryDocumentRepository : IReadFromReadModel, IWriteReadModel
    {
        private readonly ConcurrentDictionary<Guid, object> data;

        public InMemoryDocumentRepository()
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

        public T Get<T>(Guid id) where T : class, IHaveIdentity
        {
            return (T)data[id];
        }

        public IEnumerable<T> GetAll<T>() where T : class, IHaveIdentity
        {
            return data.Values.Cast<T>();
        }

        public IEnumerable<T> Query<T>(Func<T, bool> p) where T : class, IHaveIdentity
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