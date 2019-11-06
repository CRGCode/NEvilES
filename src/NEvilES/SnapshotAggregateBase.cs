using System;
using NEvilES.Abstractions;

namespace NEvilES
{
    public abstract class SnapshotAggregateBase<TStateModel> : AggregateBase, ISnapshotAggregate<TStateModel>
    {
        public TStateModel State { get; protected set; }

        public virtual void ApplySnapshot(ISnapshot<TStateModel> snapshot)
        {
            this.SetState(snapshot.Id, snapshot.Version);
            this.State = snapshot.State;
        }

        public ISnapshot<TStateModel> GetSnapshot()
        {
            return new Snapshot<TStateModel>
            {
                Id = this.Id,
                State = this.State,
                Version = this.Version,
                When = DateTimeOffset.Now
            };
        }
    }
}