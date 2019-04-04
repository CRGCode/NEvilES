using System;
using System.Collections;
using System.Collections.Generic;
using NEvilES.Pipeline;

namespace NEvilES.Extensions.DependecyInjection
{
    public class ServiceProviderFactory : IFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderFactory(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public object Get(Type type)
        {
            return _serviceProvider.GetService(type);
        }

        public object TryGet(Type type)
        {
            object instance = _serviceProvider.GetService(type);
            return instance;
        }

        public IEnumerable GetAll(Type type)
        {

            var typeToResolve = typeof(IEnumerable<>).MakeGenericType(type);
            var resolve = _serviceProvider.GetService(typeToResolve);
            return resolve as Array;
        }
    }
}