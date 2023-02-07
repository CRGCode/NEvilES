using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NEvilES.Abstractions.ObjectPath.PathElements
{
    public class EnumerableAccess : PathElementBase
    {
        public class Factory : IPathElementFactory
        {
            public IPathElement Create(string path, out string newPath)
            {
                var matches = Regex.Matches(path, @"^\[(\d+)\]");
                var match = matches[0];
                //0 is the whole match
                var index = int.Parse(match.Groups[1].Value); //the regex guarantees that the second group is an integer, so no further check is needed
                newPath = path.Remove(0, match.Value.Length);
                return new EnumerableAccess(index);
            }

            public bool IsApplicable(string path)
            {
                return Regex.IsMatch(path, @"^\[\d+\]");
            }
        }

        private readonly int index;

        public EnumerableAccess(int index)
        {
            this.index = index;
        }

        public override object Apply(object target)
        {
            //index lower than 0 doesn't have to be checked, because the IsApplicable check doesn't apply to negative values

            if (target is IEnumerable enumerable)
            {
                var i = 0;
                foreach (var value in enumerable)
                {
                    if (i == index)
                        return value;
                    i++;
                }
                //if no value is returned by now, it means that the index is too high
                throw new IndexOutOfRangeException($"The index {index} is too high. Maximum index is {i - 1}.");
            }
            else
            {
                //if the object is no enumerable, it may have an indexer
                var indexProperties = target.GetType().GetRuntimeProperties().Where(p => p.GetIndexParameters().Length > 0);
                var appropriateIndexProperty = indexProperties.FirstOrDefault(p => p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(int));
                if (appropriateIndexProperty == null) throw new ArgumentException("The target does not have an indexer that takes exactly 1 int parameter");
                return appropriateIndexProperty.GetValue(target, new object[] { index });
            }
        }
    }
}
