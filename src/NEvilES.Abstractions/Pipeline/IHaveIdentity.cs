using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NEvilES.Abstractions.Pipeline
{
    public interface IHaveIdentity<out TId>
    {
        TId Id { get; }
    }

    public interface IReadFromReadModel<in TId>
    {
        T Get<T>(TId id) where T : class, IHaveIdentity<TId>;

        IEnumerable<T> GetAll<T>() where T : class, IHaveIdentity<TId>;

        IEnumerable<T> Query<T>(Func<T, bool> p) where T : class, IHaveIdentity<TId>;
    }

    public interface IWriteReadModel<in TId>
    {
        void Insert<T>(T item) where T : class, IHaveIdentity<TId>;
        void Update<T>(T item) where T : class, IHaveIdentity<TId>;
        void Delete<T>(T item) where T : class, IHaveIdentity<TId>;
    }

    public static class SimpleMapper
    {
        public static void Map(object src, object dst)
        {
            var sourceProps = src.GetType().GetTypeInfo().GetProperties().Where(x => x.CanRead).ToList();
            var dstProps =  dst.GetType().GetTypeInfo().GetProperties().Where(x => x.CanWrite).ToList();

            foreach (var sourceProp in sourceProps)
            {
                if (dstProps.All(x => x.Name != sourceProp.Name))
                    continue;
                var p = dstProps.First(x => x.Name == sourceProp.Name);
                p.SetValue(dst, sourceProp.GetValue(src, null), null);
            }
        }
    }

}