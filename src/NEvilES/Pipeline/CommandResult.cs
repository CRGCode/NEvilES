﻿using System.Collections.Generic;
using System.Linq;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Pipeline
{
   public class CommandResult : ICommandResult
    {
        public List<IAggregateCommit> UpdatedAggregates { get; } = new List<IAggregateCommit>();
        public List<object> ReadModelItems { get; }

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
            return new AggregateCommit(UpdatedAggregates[0].StreamId, context.Transaction.Id, UpdatedAggregates.SelectMany(x => x.UpdatedEvents).ToArray());
        }

        public T FindProjectedItem<T>() where T : class
        {
            return ReadModelItems.Where(x =>
            {
                var memberInfo = typeof(T);
                var type = x.GetType();
                return type == memberInfo || type.IsSubclassOf(memberInfo);
            }).Cast<T>().FirstOrDefault();
        }

        public IEnumerable<T> FindProjectedItems<T>() where T : class
        {
            return ReadModelItems.Where(x => {
                var memberInfo = typeof(T);
                var type = x.GetType();
                return type == memberInfo || type.IsSubclassOf(memberInfo);
            }).Cast<T>();
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
