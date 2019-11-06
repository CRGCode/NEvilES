using System;

namespace NEvilES.Abstractions
{
    public interface ISnapshotAggregate<TStateModel>
    {
        TStateModel State { get; }
        ISnapshot<TStateModel> GetSnapshot();
        void ApplySnapshot(ISnapshot<TStateModel> snapshot);
    }

    public interface ISnapshot<TState>
    {
        Guid Id { get; set; }
        int Version { get; set; }
        TState State { get; set; }
        DateTimeOffset When { get; set; }
    }

    public class Snapshot<TState> : ISnapshot<TState>
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public TState State { get; set; }
        public DateTimeOffset When { get; set; }
    }
}