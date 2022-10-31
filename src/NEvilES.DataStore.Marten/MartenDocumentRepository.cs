using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Marten;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.Marten
{
    public abstract class MartenDocumentRepository<TId> : IReadFromReadModel<TId>, IWriteReadModel<TId> 
    {
        private readonly IDocumentSession session;

        protected MartenDocumentRepository(IDocumentSession documentSession)
        {
            session = documentSession;
        }

        public void Insert<T>(T item) where T : class, IHaveIdentity<TId>
        {
            session.Insert(item);
            session.SaveChanges();
        }

        public void Update<T>(T item) where T : class, IHaveIdentity<TId>
        {
            session.Store(item);
            session.SaveChanges();
        }

        public void Delete<T>(T item) where T : class, IHaveIdentity<TId>
        {
            session.Delete(item);
            session.SaveChanges();
        }

        public T Get<T>(TId id) where T : class, IHaveIdentity<TId>
        {
            var dynamicId = (dynamic)id;

            return session.Load<T>(dynamicId);
        }

        public IEnumerable<T> GetAll<T>() where T : class, IHaveIdentity<TId>
        {
            throw new NotImplementedException("This is not a good idea and is only implemented - for ");
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, bool>> p) where T : class, IHaveIdentity<TId>
        {
            return session.Query<T>().Where(p);
        }

        public IQueryable<T> GetQuery<T>(Expression<Func<T, bool>> p) where T : class, IHaveIdentity<TId>
        {
            return session.Query<T>().Where(p);
        }
    }

    public class DocumentRepositoryWithKeyTypeGuid : MartenDocumentRepository<Guid>
    {
        public DocumentRepositoryWithKeyTypeGuid(IDocumentSession documentSession) : base(documentSession)
        {
        }
    }

    public class DocumentRepositoryWithKeyTypeString : MartenDocumentRepository<string>
    {
        public DocumentRepositoryWithKeyTypeString(IDocumentSession documentSession) : base(documentSession)
        {
        }
    }
}