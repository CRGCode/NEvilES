using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NEvilES.Abstractions.ObjectPath.PathElements
{
    public class DictionaryAccess : PathElementBase
    {
        public class Factory : IPathElementFactory
        {
            public IPathElement Create(string path, out string newPath)
            {
                var matches = Regex.Matches(path, @"^\[(\w+)\]");
                var match = matches[0];
                //0 is the whole match
                var key = match.Groups[1].Value; //the regex guarantees that the second group is an integer, so no further check is needed
                newPath = path.Remove(0, match.Value.Length);
                return new DictionaryAccess(key);
            }

            public bool IsApplicable(string path)
            {
                return Regex.IsMatch(path, @"^\[\w+\]");
            }
        }

        private readonly string key;

        public DictionaryAccess(string key)
        {
            this.key = key;
        }

        public override object Apply(object target)
        {
            var dictionary = target as IDictionary;
            if (dictionary != null)
            {
                foreach (DictionaryEntry de in dictionary)
                {
                    if (de.Key.ToString() == key)
                        return de.Value;
                }

                //if no value is returned by now, it means that the index is too high
                throw new ArgumentException($"The key {key} does not exist.");
            }

            //if the object is no dictionary, it may have an indexer
            var indexProperties = target.GetType().GetRuntimeProperties().Where(p => p.GetIndexParameters().Length > 0);
            var appropriateIndexProperty = indexProperties.FirstOrDefault(p => p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(string));
            if (appropriateIndexProperty == null) throw new ArgumentException("The target does not have an indexer that takes exactly 1 string parameter");
            return appropriateIndexProperty.GetValue(target, new object[] { key });
        }
    }
}
