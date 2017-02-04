using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NEvilES.Pipeline
{
    public class CommandResult
    {
        public List<AggregateCommit> UpdatedAggregates { get; } = new List<AggregateCommit>();
        public ArrayList ReadModelItems { get; }

        public CommandResult() : this(new AggregateCommit[] { }) { }
        public CommandResult(IEnumerable<AggregateCommit> commits)
        {
            UpdatedAggregates.AddRange(commits);
            ReadModelItems = new ArrayList();
        }

        public CommandResult(params AggregateCommit[] commits) : this((IEnumerable<AggregateCommit>)commits)
        {
        }

        public CommandResult Append(AggregateCommit commit)
        {
            UpdatedAggregates.Add(commit);
            return this;
        }

        public CommandResult Append(IEnumerable<AggregateCommit> commits)
        {
            UpdatedAggregates.AddRange(commits);
            return this;
        }

        public CommandResult Add(CommandResult result)
        {
            UpdatedAggregates.AddRange(result.UpdatedAggregates);
            ReadModelItems.AddRange(result.ReadModelItems);
            return this;
        }

        public AggregateCommit ToAggregateCommit(CommandContext context)
        {
            return new AggregateCommit(UpdatedAggregates[0].StreamId, context.TransactionId, "", UpdatedAggregates.SelectMany(x => x.UpdatedEvents).ToArray());
        }

        public T FindProjectedItem<T>() where T : class
        {
            foreach (var readModelItem in ReadModelItems)
            {
                if (readModelItem.GetType() == typeof(T))
                {
                    return (T)readModelItem;
                }
            }
            return default(T);
        }

        public IEnumerable<T> FindProjectedItems<T>() where T : class
        {
            foreach (var readModelItem in ReadModelItems)
            {
                if (readModelItem.GetType() == typeof(T))
                {
                    yield return (T)readModelItem;
                }
            }
        }
    }

    public static class CommandResultExtensions
    {
        public static IEnumerable<T> FilterEvents<T>(this CommandResult result)
        {
            return result.UpdatedAggregates.SelectMany(x => x.UpdatedEvents).Select(x => x.Event).OfType<T>();
        }
    }
}
