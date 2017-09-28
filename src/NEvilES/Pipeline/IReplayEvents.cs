using System;
using System.Collections.Generic;
using System.Dynamic;

namespace NEvilES.Pipeline
{
    public interface IHaveIdentity
    {
        Guid Id { get; }
    }

    public interface IReadFromReadModel
    {
        T Get<T>(Guid id) where T : IHaveIdentity;
        IEnumerable<T> Query<T>(Func<T,bool> p);
    }

    public interface IWriteReadModel
    {
        void Insert<T>(T item) where T : class, IHaveIdentity;
        void Update<T>(T item) where T : class, IHaveIdentity;
    }

    public static class ReplayEvents
    {
        public static void Replay(IFactory factory, IAccessDataStore reader,  Int64 from = 0, Int64 to = 0)
        {
            foreach (var commit in reader.Read(from,to))
            {
                ReadModelProjectorHelper.Project(new CommandResult(commit), factory, CommandContext.Null());
            }
        }
    }

    public interface IAccessDataStore
    {
        IEnumerable<AggregateCommit> Read(Int64 from = 0, Int64 to = 0);
        IEnumerable<AggregateCommit> Read(Guid streamId);
        IEnumerable<AggregateCommit> ReadLatestLimit(Guid streamId, int limit = 50);
    }
}