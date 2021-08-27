using System;
using System.Collections.Generic;
using System.Linq;
using Marten;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.DataStore.Marten
{
    public class MartenDocumentRepository : IReadFromReadModel, IWriteReadModel
    {
        private readonly IDocumentSession session;

        public MartenDocumentRepository(IDocumentSession documentSession)
        {
            session = documentSession;
        }

        public void Insert<T>(T item) where T : class, IHaveIdentity
        {
            session.Insert(item);
            session.SaveChanges();
        }

        public void Update<T>(T item) where T : class, IHaveIdentity
        {
            session.Store(item);
            session.SaveChanges();
        }

        public void Delete<T>(T item) where T : class, IHaveIdentity
        {
            session.Delete(item);
            session.SaveChanges();
        }

        public T Get<T>(Guid id) where T : class, IHaveIdentity
        {
            return session.Load<T>(id);
        }

        public IEnumerable<T> GetAll<T>() where T : class, IHaveIdentity
        {
            throw new NotImplementedException("This is not a good idea and is only implemented - for ");
        }

        public IEnumerable<T> Query<T>(Func<T, bool> p) where T : class, IHaveIdentity
        {
            return session.Query<T>().Where(p);
        }
    }
}