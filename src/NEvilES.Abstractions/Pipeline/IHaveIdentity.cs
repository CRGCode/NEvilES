using System;
using System.Collections.Generic;

namespace NEvilES.Abstractions.Pipeline
{
    public interface IHaveIdentity
    {
        Guid Id { get; }
    }

    public interface IReadFromReadModel
    {
        T Get<T>(Guid id) where T : class, IHaveIdentity;

        IEnumerable<T> GetAll<T>() where T : class, IHaveIdentity;

        IEnumerable<T> Query<T>(Func<T, bool> p) where T : class, IHaveIdentity;
    }


    public interface IWriteReadModel
    {
        void Insert<T>(T item) where T : class, IHaveIdentity;
        void Update<T>(T item) where T : class, IHaveIdentity;
        void Delete<T>(T item) where T : class, IHaveIdentity;
    }

}