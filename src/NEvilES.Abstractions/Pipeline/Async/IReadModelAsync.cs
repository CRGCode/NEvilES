using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NEvilES.Abstractions.Pipeline.Async
{
    public interface IReadFromReadModelAsync
    {
        Task<T> GetAsync<T>(Guid id) where T : IHaveIdentity;
        Task<IEnumerable<T>> QueryAsync<T>(Func<T, bool> p);
    }


    public interface IWriteReadModelAsync
    {
        Task InsertAsync<T>(T item) where T : class, IHaveIdentity;
        Task UpdateAsync<T>(T item) where T : class, IHaveIdentity;
        Task SaveAsync<T>(T item) where T : class, IHaveIdentity;
        Task DeleteAsync<T>(T item) where T : class, IHaveIdentity;
    }
}