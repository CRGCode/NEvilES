using System.Collections.Generic;

namespace NEvilES.Abstractions.Pipeline
{
    public interface ICommandResult : IReadModelItems
    {
        List<IAggregateCommit> UpdatedAggregates { get; }

        ICommandResult Append(IAggregateCommit commit);
        ICommandResult Append(IEnumerable<IAggregateCommit> commits);
        ICommandResult Add(ICommandResult result);
        IAggregateCommit ToAggregateCommit(ICommandContext context);
 }

    public interface IReadModelItems
    {
        List<object> ReadModelItems { get; }
        T FindProjectedItem<T>() where T : class;
        IEnumerable<T> FindProjectedItems<T>() where T : class;
    }
}