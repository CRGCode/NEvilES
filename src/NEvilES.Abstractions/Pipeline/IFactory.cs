using System;
using System.Collections;

namespace NEvilES.Abstractions.Pipeline
{
    public interface IFactory
    {
        object Get(Type type);
        object TryGet(Type type);
        IEnumerable GetAll(Type type);
    }
}
