using System.Collections.Generic;

namespace NEvilES.Abstractions.Pipeline
{
    public interface ICommandResult
    {
        List<IAggregateCommit> UpdatedAggregates { get; }
        List<object> ReadModelItems { get; }

        ICommandResponse Response { get; }
        ICommandResult Append(IAggregateCommit commit);
        ICommandResult Append(IEnumerable<IAggregateCommit> commits);
        ICommandResult Add(ICommandResult result);
        IAggregateCommit ToAggregateCommit(ICommandContext context);
        T FindProjectedItem<T>() where T : class;
        IEnumerable<T> FindProjectedItems<T>() where T : class;
    }
}