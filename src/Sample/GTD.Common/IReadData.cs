using System;

namespace GTD.Common
{
    public interface IReadData
    {
        T Get<T>(Guid id) where T : IHaveIdentity;
    }

    public interface IHaveIdentity
    {
        Guid Id { get; }
    }
}
