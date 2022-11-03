using System;
using System.Threading.Tasks;

namespace NEvilES.Abstractions.Pipeline
{
    public interface IProject<in T> where T : IEvent
    {
        void Project(T message, IProjectorData data);
    }

    public interface IProjectAsync<in T> where T : IEvent
    {
        Task ProjectAsync(T message, IProjectorData data);
    }

    public interface IProjectWithResult<in T> where T : IEvent
    {
        IProjectorResult Project(T message, IProjectorData data);
    }

    public interface IProjectWithResultAsync<in T> where T : IEvent
    {
        Task<IProjectorResult> ProjectAsync(T message, IProjectorData data);
    }


    public interface IProjectorResult
    {
        object[] Items { get; }
    }

    public interface IProjectorData
    {
        Guid StreamId { get; }
        ICommandContext CommandContext { get; }
        IUser By { get; }
        Type Type { get; }
        object Event { get; }
        DateTime TimeStamp { get; }
        int Version { get; }
    }
}
