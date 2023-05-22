using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NEvilES.Abstractions.ObjectPath.PathElements
{
    public class Property : PathElementBase
    {
        public class Factory : IPathElementFactory
        {
            public IPathElement Create(string path, out string newPath)
            {
                var property = Regex.Matches(path, @"^\w+")[0].Value;
                newPath = path.Remove(0, property.Length);
                return new Property(property);
            }

            public bool IsApplicable(string path)
            {
                return Regex.IsMatch(path, @"^\w+");
            }
        }

        private readonly string property;
        private object target;
        private PropertyInfo pi;

        public Property(string property)
        {
            this.property = property;
        }

        public override object Apply(object target)
        {
            var p = target.GetType().GetRuntimeProperty(property);
            if (p == null)
                throw new ArgumentException($"The property {property} could not be found.");

            pi = p;
            Value = p.GetValue(target);
            this.target = target;
            return this;
            //var result = p.GetValue(target);
            //return result;
        }

        public object Value { get; set; }

        public void SetValue(object value)
        {
            var pt = pi.PropertyType;
            if (pt.IsEnum)
            {
                pi.SetValue(target, Enum.Parse(pt, (string) value), null);
                return;
            }
            if (pt  == typeof(decimal))
            {
                pi.SetValue(target, decimal.Parse((string) value), null);
                return;
            }

            if (pt  == typeof(int))
            {
                pi.SetValue(target, int.Parse((string) value), null);
                return;
            }

            if (pt  == typeof(DateTime))
            {
                pi.SetValue(target, DateTime.Parse((string) value), null);
                return;
            }
    
            pi.SetValue(target,value);
        }
    }
}
