using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NEvilES.DataStore
{
    // https://arbel.net/2013/02/03/best-practices-for-using-concurrentdictionary/
    // http://www.c-sharpcorner.com/UploadFile/ff2f08/anonymous-types-in-C-Sharp/

    public interface IWriteData
    {
        void Insert<T>(T item);
        void Update<T>(T item, object changes = null);
    }

    public interface IWriteThruCache
    {
        void Insert<T>(T item, IWriteData dataWriter);
        void Update<T>(T item, IWriteData dataWriter, object changes = null);
    }

    public interface IReadFromCache // Not Really needed...
    {
        IEnumerable<T> Lookup<T, TCache>(Func<TCache, Cache<T>.Index> indexFunc) where TCache : Cache<T>;
    }

    public class WriteThruCache : IWriteThruCache
    {
        public Dictionary<Type, object> TypesCached = new Dictionary<Type, object>();

        public void RegisterType<T, TCache>(TCache cache, IEnumerable<T> initialData) where TCache : Cache<T>
        {
            var type = typeof(T);
            if (TypesCached.ContainsKey(type))
                throw new Exception("Already exists");
            TypesCached.Add(type, cache);
            cache.Load(initialData);
        }

        private Cache<T> GetCache<T>()
        {
            var type = typeof(T);
            if (!TypesCached.ContainsKey(type))
                return null;
            return (Cache<T>)TypesCached[type];
        }

        public void Insert<T>(T item, IWriteData dataWriter)
        {
            var cache = GetCache<T>();
            if (cache == null)
            {
                dataWriter.Insert(item);
                return;
            }
            cache.AddToIndex(item);
            dataWriter.Insert(item);
            cache.AddToPostIndex(item);
        }

        public void Update<T>(T item, IWriteData dataWriter, object changes = null)
        {
            var cache = GetCache<T>();
            cache?.UpdateIndex(item);
            dataWriter.Update(item, changes);
        }

        public T Load<T>(Guid id)
        {
            var cache = GetCache<T>();
            var results = cache.IndexFrom(new { Id = id }).GetValues().Result; // This doesn't work!
            return results.First();
        }

        public IEnumerable<T> Lookup<T, TCache>(Func<TCache, Cache<T>.Index> indexFunc) where TCache : Cache<T>
        {
            var cache = (TCache)GetCache<T>();
            return indexFunc(cache).GetValues().Result;
        }
    }

    public abstract class Cache<T>
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private ConcurrentDictionary<object, List<T>> indexes;
        private ConcurrentDictionary<T, List<object>> reverseIndexes;

        protected abstract IEnumerable<Index> CreateIndexes(T value);
        protected virtual IEnumerable<Index> CreatePostIndexes(T value)
        {
            yield break;
        }

        public void Load(IEnumerable<T> values)
        {
            //_indexes = new ConcurrentDictionary<object, List<TValue>>(values
            //    .SelectMany(value => indexDefs(value).Select(IndexFrom).Concat(CreatePostIndexes(value)), (value, index) => new { value, index })
            //    .GroupBy(x => x.index.Key, x => x.value)
            //    .Select(g => new { g.Key, Values = g.ToList() })
            //    .ToDictionary(x => x.Key, x => x.Values));

            indexes = new ConcurrentDictionary<object, List<T>>(
                (from value in values
                 from index in CreateIndexes(value).Concat(CreatePostIndexes(value))
                 group value by index.Key into g
                 select new { g.Key, Values = g.ToList() })
                    .ToDictionary(x => x.Key, x => x.Values)
                );

            reverseIndexes = new ConcurrentDictionary<T, List<object>>(
                (from kv in indexes
                 from value in kv.Value
                 group kv.Key by value into g
                 select new { value = g.Key, Keys = g.ToList() })
                    .ToDictionary(x => x.value, x => x.Keys));
        }

        public void AddToIndex(T value)
        {
            AddToIndex(value, CreateIndexes(value).ToArray());
        }

        public void AddToPostIndex(T value)
        {
            AddToIndex(value, CreatePostIndexes(value).ToArray());
        }

        private void AddToIndex(T value, Index[] toAdd)
        {
            foreach (var index in toAdd)
            {
                var list = indexes.GetOrAdd(index.Key, new List<T>());

                lock (list)
                {
                    list.Add(value);
                }
            }

            reverseIndexes.GetOrAdd(value, new List<object>()).AddRange(toAdd.Select(x => x.Key));
        }

        public void UpdateIndex(T value)
        {
            List<object> objects;
            if (reverseIndexes.TryRemove(value, out objects))
            {
                foreach (var index in objects)
                {
                    indexes.AddOrUpdate(index, _ => new List<T>(), (_, list) =>
                    {
                        lock (list)
                        {
                            list.Remove(value);
                        }
                        return list;
                    });
                }
            }
            AddToIndex(value);
            AddToPostIndex(value);
        }

        public Index IndexFrom(object key)
        {
            return new Index(this, key, () => new List<T>());
        }

        public Index IndexFrom<TKey>(TKey key, Func<TKey, IEnumerable<T>> loader)
        {
            return new Index(this, key, () => loader(key));
        }

        readonly ConcurrentDictionary<T, Task> valuesPendingTasks = new ConcurrentDictionary<T, Task>();
        protected virtual async Task<IEnumerable<T>> AddIfEmptyAsync(Index index, Func<T> valueFactory, Func<T, Task> whenAdded)
        {
            T value;
            Task task;

            _lock.EnterUpgradeableReadLock();
            try
            {
                var loader = index.GetValues();
                loader.Wait();
                var values = loader.Result.ToArray();
                if (values.Any())
                    return values;

                _lock.EnterWriteLock();
                try
                {
                    value = valueFactory();
                    if (!CreateIndexes(value).Select(x => x.Key).Contains(index.Key))
                    {
                        //DataException doesn't exist .netcore
                        //throw new DataException("Value to be added does not match the specified index");
                        throw new Exception("Value to be added does not match the specified index");
                    }

                    task = whenAdded(value);
                    valuesPendingTasks.TryAdd(value, task);
                    AddToIndex(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }

            await task.ConfigureAwait(false);
            AddToPostIndex(value);
            valuesPendingTasks.TryRemove(value, out task);
            return new[] { value };
        }

        public class Index
        {
            private readonly Cache<T> lookup;
            private readonly Func<IEnumerable<T>> loader;

            public Index(Cache<T> lookup, object key, Func<IEnumerable<T>> loader)
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key");
                }

                this.lookup = lookup;
                this.loader = loader;
                Key = key;
            }

            public TaskAwaiter<IEnumerable<T>> GetAwaiter()
            {
                return GetValues().GetAwaiter();
            }

            public object Key { get; }
            public async Task<IEnumerable<T>> GetValues()
            {
                var values = lookup.indexes.GetOrAdd(Key, _ => loader().ToList());
                var tasks = values.Select(GetPendingTask).Where(x => x != null).ToArray();
                await Task.WhenAll(tasks).ConfigureAwait(false);
                return values.ToArray();
            }

            private Task GetPendingTask(T value)
            {
                Task task;
                lookup.valuesPendingTasks.TryGetValue(value, out task);
                return task;
            }

            public Task<IEnumerable<T>> AddIfEmptyAsync(Func<T> valueFactory, Func<T, Task> ifNew = null)
            {
                return lookup.AddIfEmptyAsync(this, valueFactory, ifNew ?? (x => Task.FromResult(0)));
            }
        }
    }
}