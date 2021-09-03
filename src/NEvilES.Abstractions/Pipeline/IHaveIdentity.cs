using System;
using System.Collections.Generic;

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
}