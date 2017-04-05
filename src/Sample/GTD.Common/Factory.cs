using System;
using System.Collections;
using System.Collections.Generic;
using Autofac;
using NEvilES.Pipeline;

namespace GTD.Common
{
    public class Factory : IFactory
    {
        private readonly ILifetimeScope lifetimeScope;

        public Factory(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }

        public object Get(Type type)
        {
            return lifetimeScope.Resolve(type);
        }

        public object TryGet(Type type)
        {
            object instance;
            lifetimeScope.TryResolve(type, out instance);
            return instance;
        }

        public IEnumerable GetAll(Type type)
        {
            var typeToResolve = typeof(IEnumerable<>).MakeGenericType(type);
            var resolve = lifetimeScope.Resolve(typeToResolve);
            return resolve as Array;
        }
    }
}