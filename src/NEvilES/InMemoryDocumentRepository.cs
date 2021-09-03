using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES
{
    public abstract class InMemoryDocumentRepository<TId> : IReadFromReadModel<TId>, IWriteReadModel<TId>
    {
        private readonly ConcurrentDictionary<string, object> data;

        public InMemoryDocumentRepository()
        {
            data = new ConcurrentDictionary<string, object>();
        }

        public void Insert<T>(T item) where T : class, IHaveIdentity<TId>
        {
            data.TryAdd($"{typeof(T).Name}_{item.Id}", item);
        }

        public void Update<T>(T item) where T : class, IHaveIdentity<TId>
        {
            if (data.ContainsKey($"{typeof(T).Name}_{item.Id}"))
            {
                data[$"{typeof(T).Name}_{item.Id}"] = item;
            }
            else
            {
                data.TryAdd($"{typeof(T).Name}_{item.Id}", item);
            }
        }

        public void Delete<T>(T item) where T : class, IHaveIdentity<TId>
        {
            data.TryRemove($"{typeof(T).Name}_{item.Id}", out _);
        }

        public T Get<T>(TId id) where T : class, IHaveIdentity<TId>
        {
            return (T)data[$"{typeof(T).Name}_{id}"];
        }

        public IEnumerable<T> GetAll<T>() where T : class, IHaveIdentity<TId>
        {
            return data.Values.Where(x => x.GetType() == typeof(T)).Cast<T>();
        }

        public IEnumerable<T> Query<T>(Func<T, bool> p) where T : class, IHaveIdentity<TId>
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

        public IEnumerable<object> GetAll()
        {
            return data.Values;
        }
    }

    public class DocumentStoreGuid : InMemoryDocumentRepository<Guid> { }
    public class DocumentStoreString : InMemoryDocumentRepository<string> { }
}