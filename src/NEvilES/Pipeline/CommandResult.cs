using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
    public class CommandResult : ICommandResult
    {
        private ICommandResponse _commandResponse;

        public List<IAggregateCommit> UpdatedAggregates { get; } = new List<IAggregateCommit>();
        public List<object> ReadModelItems { get; }

        public ICommandResponse Response => _commandResponse;

        public CommandResult() : this(new IAggregateCommit[] { }) { }
        public CommandResult(IEnumerable<IAggregateCommit> commits)
        {
            UpdatedAggregates.AddRange(commits);
            ReadModelItems = new List<object>();
        }

        public CommandResult(params IAggregateCommit[] commits) : this((IEnumerable<IAggregateCommit>)commits)
        {
        }

        public ICommandResult Append(IAggregateCommit commit)
        {
            UpdatedAggregates.Add(commit);
            return this;
        }

        public ICommandResult Append(IEnumerable<IAggregateCommit> commits)
        {
            UpdatedAggregates.AddRange(commits);
            return this;
        }

        public ICommandResult Add(ICommandResult result)
        {
            UpdatedAggregates.AddRange(result.UpdatedAggregates);
            ReadModelItems.AddRange(result.ReadModelItems);
            return this;
        }

        public IAggregateCommit ToAggregateCommit(ICommandContext context)
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

        public void SetCommandResponse(ICommandResponse res)
        {
            _commandResponse = res;
        }
    }

    public static class CommandResultExtensions
    {
        public static IEnumerable<T> FilterEvents<T>(this ICommandResult result)
        {
            return result.UpdatedAggregates.SelectMany(x => x.UpdatedEvents).Select(x => x.Event).OfType<T>();
        }
    }
}
