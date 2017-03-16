using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NEvilES.Pipeline
{
    public class CommandResult
    {
        public List<AggregateCommit> UpdatedAggregates { get; } = new List<AggregateCommit>();
        public List<object> ReadModelItems { get; }

        public CommandResult() : this(new AggregateCommit[] { }) { }
        public CommandResult(IEnumerable<AggregateCommit> commits)
        {
            UpdatedAggregates.AddRange(commits);
            ReadModelItems = new List<object>();
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
            return new AggregateCommit(UpdatedAggregates[0].StreamId, context.Transaction.Id, "", UpdatedAggregates.SelectMany(x => x.UpdatedEvents).ToArray());
        }

        public T FindProjectedItem<T>() where T : class
        {
            return ReadModelItems.Where(x => x.GetType() == typeof(T)).Cast<T>().FirstOrDefault();
        }

        public IEnumerable<T> FindProjectedItems<T>() where T : class
        {
            return ReadModelItems.Where(x => x.GetType() == typeof(T)).Cast<T>();
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
