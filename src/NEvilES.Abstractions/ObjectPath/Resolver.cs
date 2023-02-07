using System;
using System.Collections.Generic;
using System.Linq;
using NEvilES.Abstractions.ObjectPath.PathElements;

namespace NEvilES.Abstractions.ObjectPath
{
    public class Resolver
    {
        private IList<IPathElementFactory> pathElementFactories;
        /// <summary>
        /// contains the path element factories used to resolve given paths
        /// more specific factories must be before more generic ones, because the first applicable one is taken
        /// </summary>
        public IList<IPathElementFactory> PathElementFactories
        {
            get => pathElementFactories;
            set => pathElementFactories = value ?? throw new ArgumentNullException("The PathElementFactories must not be null");
        }

        public Resolver()
        {
            PathElementFactories = new List<IPathElementFactory>
            {
                new Property.Factory(),
                new EnumerableAccess.Factory(),
                new DictionaryAccess.Factory(),
                new SelectionAccess.Factory()
            };
        }

        public IList<IPathElement> CreatePath(string path)
        {
            var pathElements = new List<IPathElement>();
            var tempPath = path;
            while (tempPath.Length > 0)
            {
                var pathElement = createPathElement(tempPath, out tempPath);
                pathElements.Add(pathElement);
                //remove the dots chaining properties 
                //no PathElement could do this reliably
                //the only appropriate one would be Property, but there doesn't have to be a dot at the beginning (if it is the first PathElement, e.g. "Property1.Property2")
                //and I don't want that knowledge in PropertyFactory
                if (tempPath.StartsWith("."))
                    tempPath = tempPath.Remove(0, 1);
            }
            return pathElements;
        }

        public object Resolve(object target, string path)
        {
            var pathElements = CreatePath(path);
            return Resolve(target, pathElements);
        }

        public object Resolve(object target, IList<IPathElement> pathElements)
        {
            var tempResult = target;
            object apply = null;
            foreach (var pathElement in pathElements)
            {
                if (tempResult is Selection result)
                {
                    tempResult = pathElement.Apply(result);;
                }
                else
                {
                    apply = pathElement.Apply(tempResult);
                    tempResult = apply;

                    if (pathElement is Property)
                    {
                        tempResult = ((Property)tempResult).Value;
                    }
                }
            }

            if (tempResult is Selection selection)
                return selection.AsEnumerable();
            else
                return apply;
        }

        private IPathElement createPathElement(string path, out string newPath)
        {
            //get the first applicable path element type
            var pathElementFactory = PathElementFactories.FirstOrDefault(f => f.IsApplicable(path));

            if (pathElementFactory == null)
                throw new InvalidOperationException($"There is no applicable path element factory for {path}");

            var result = pathElementFactory.Create(path, out newPath);
            return result;
        }

        public object ResolveSafe(object target, string path)
        {
            try
            {
                return Resolve(target, path);
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
    }
}
