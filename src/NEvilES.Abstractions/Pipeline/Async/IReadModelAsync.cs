using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NEvilES.Abstractions.Pipeline.Async
{
    public interface IReadFromReadModelAsync<in TId>
    {
        Task<T> GetAsync<T>(Guid id) where T : IHaveIdentity<TId>;
        Task<IEnumerable<T>> QueryAsync<T>(Func<T, bool> p);
    }


    public interface IWriteReadModelAsync<in TId>
    {
        Task InsertAsync<T>(T item) where T : class, IHaveIdentity<TId>;
        Task UpdateAsync<T>(T item) where T : class, IHaveIdentity<TId>;
        Task SaveAsync<T>(T item) where T : class, IHaveIdentity<TId>;
        Task DeleteAsync<T>(T item) where T : class, IHaveIdentity<TId>;
    }
}