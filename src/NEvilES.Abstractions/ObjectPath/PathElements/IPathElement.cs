using System.Collections;

namespace NEvilES.Abstractions.ObjectPath.PathElements
{
    public interface IPathElement
    {
        object Apply(object target);
        IEnumerable Apply(Selection target);
    }
}
