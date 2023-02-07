using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NEvilES.Abstractions.ObjectPath.PathElements
{
    public class SelectionAccess : IPathElement
    {
        public class Factory : IPathElementFactory
        {
            private const string selectionIndicator = "[]";
            public IPathElement Create(string path, out string newPath)
            {
                newPath = path.Remove(0, selectionIndicator.Length);
                return new SelectionAccess();
            }

            public bool IsApplicable(string path)
            {
                return path.StartsWith(selectionIndicator);
            }
        }

        public object Apply(object target)
        {
            var enumerable = target as IEnumerable;
            var result = new Selection(enumerable);
            return result;
        }

        public IEnumerable Apply(Selection target)
        {
            var results = new List<object>();
            foreach(var entry in target.Entries)
            {
                if (!(entry is IEnumerable enumerable))
                    results.Add(entry);
                else
                {
                    results.AddRange(enumerable.Cast<object>());
                }
            }
            var result = new Selection(results);
            return result;
        }
    }
}
