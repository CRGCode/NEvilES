using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Marten;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.Marten
{
    public abstract class MartenDocumentRepository<TId> : IReadFromReadModel<TId>, IWriteReadModel<TId>, IQueryFromReadModel<TId>, IDisposable
    {
        private readonly IDocumentSession session;
        private bool changed;

        protected MartenDocumentRepository(IDocumentSession documentSession)
        {
            session = documentSession;
        }

        public void Insert<T>(T item) where T : class, IHaveIdentity<TId>
        {
            session.Insert(item);
            changed = true;
        }

        public void Update<T>(T item) where T : class, IHaveIdentity<TId>
        {
            session.Store(item);
            changed = true;
        }

        public void Delete<T>(T item) where T : class, IHaveIdentity<TId>
        {
            session.Delete(item);
            changed = true;
        }

        public T Get<T>(TId id) where T : class, IHaveIdentity<TId>
        {
            var dynamicId = (dynamic)id;

            return session.Load<T>(dynamicId);
        }

        public IQueryable<T> GetAll<T>() where T : class, IHaveIdentity<TId>
        {
            return session.Query<T>();
        }

        public IEnumerable<T> Query<T>(Expression<Func<T, bool>> p) where T : class, IHaveIdentity<TId>
        {
            return session.Query<T>().Where(p);
        }

        public IQueryable<T> GetQuery<T>(Expression<Func<T, bool>> p) where T : class, IHaveIdentity<TId>
        {
            return session.Query<T>().Where(p);
        }

        
        public void WipeDocTypeIfExists<T>()
        {
            session.DocumentStore.Advanced.Clean.DeleteDocumentsByTypeAsync(typeof(T)).Wait();
            changed = true;
        }

        public void Dispose()
        {
            if (session == null) 
                return;
            if (changed)
            {
                session.SaveChanges();
                changed = false;
            }

            session.Dispose();
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