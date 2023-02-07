using System;
using System.Collections;
using System.Collections.Generic;
using NEvilES.Abstractions.Pipeline;

namespace NEvilES
{
    public class ServiceProviderFactory : IFactory
    {
        private readonly IServiceProvider sp;

        public ServiceProviderFactory(IServiceProvider serviceProvider)
        {
            sp = serviceProvider;
        }

        public object Get(Type type)
        {
            return sp.GetService(type);
        }

        public object TryGet(Type type)
        {
            var instance = sp.GetService(type);
            return instance;
        }

        public IEnumerable GetAll(Type type)
        {

            var typeToResolve = typeof(IEnumerable<>).MakeGenericType(type);
            var resolve = sp.GetService(typeToResolve);
            return resolve as Array;
        }
    }

    //public class ScopedServiceProviderFactory : IFactory
    //{
    //    private readonly IServiceProvider scope;

    //    public ScopedServiceProviderFactory(IServiceProvider serviceProvider)
    //    {
    //        scope = serviceProvider;
    //    }

    //    public object Get(Type type)
    //    {
    //        return scope.GetService(type);
    //    }

    //    public object TryGet(Type type)
    //    {
    //        var instance = scope.GetService(type);
    //        return instance;
    //    }

    //    public IEnumerable GetAll(Type type)
    //    {
    //        var typeToResolve = typeof(IEnumerable<>).MakeGenericType(type);
    //        var resolve = scope.GetService(typeToResolve);
    //        return resolve as Array;
    //    }
    //}
}