using System.Collections;
using System.Collections.Generic;

namespace NEvilES.Abstractions.ObjectPath.PathElements
{
    public abstract class PathElementBase : IPathElement
    {
        public IEnumerable Apply(Selection target)
        {
            var results = new List<object>();
            foreach (var entry in target.Entries)
            {
                var value = Apply(entry);
                if (value is Property prop)
                {
                    value = prop.Value;
                }
                results.Add(value);
            }
            var result = new Selection(results);
            return result;
        }

        public abstract object Apply(object target);
    }
}
