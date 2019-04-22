using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Abstractions.Pipeline.Async;
using NEvilES.Pipeline;

namespace NEvilES.Extensions.DependecyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static Dictionary<Assembly, Type[]> _types = new Dictionary<Assembly, Type[]>();
        public interface IRegisteredTypesBuilder
        {
            IRegisteredTypesBuilder ConnectImplementingType(Type interfaceType);
        }
        public class RegisteredTypesBuilder : IRegisteredTypesBuilder
        {
            private IEnumerable<Type> _assemblyTypes;
            private IServiceCollection _services;
            public RegisteredTypesBuilder(IEnumerable<Type> assemblyTypes, IServiceCollection services)
            {
                _assemblyTypes = assemblyTypes;
                _services = services;
            }

            public IRegisteredTypesBuilder ConnectImplementingType(Type interfaceType)
            {
                string name = interfaceType.Name;
                foreach (var item in _assemblyTypes)
                {
                    var interfaces = item.GetInterfaces();
                    if (item.IsAbstract || !item.IsClass || interfaces == null) continue;

                    var matchingInterfaces = interfaces.Where(t => t.Name == name);

                    if (matchingInterfaces.Count() == 0) continue;

                    _services = ConnectImplementingTypes(_services, item, matchingInterfaces);
                }

                return this;
            }

            public IServiceCollection Build() => _services;

            private IServiceCollection ConnectImplementingTypes(IServiceCollection services, Type implementingType, IEnumerable<Type> interfaceTypes)
            {
                services.AddScoped(implementingType);
                foreach (var t in interfaceTypes)
                {
                    services.AddScoped(t, s => s.GetRequiredService(implementingType));
                }
                return services;
            }
        }

        public static IRegisteredTypesBuilder RegisterTypesFrom(this IServiceCollection services, params Type[] assemblyType)
            => RegisterTypesFrom(services, assemblyType);

        public static IRegisteredTypesBuilder RegisterTypesFrom(this IServiceCollection services, IEnumerable<Type> assemblyType)
        {
            List<Assembly> assemblies = new List<Assembly>();
            lock (_types)
            {
                foreach (var type in assemblyType)
                {
                    if (!_types.ContainsKey(type.Assembly))
                    {
                        _types.Add(type.Assembly, type.Assembly.GetTypes());
                    }
                    assemblies.Add(type.Assembly);
                }

            }

            return new RegisteredTypesBuilder(assemblies.SelectMany<Assembly, Type>(x => _types[x]), services);
        }

        public static IServiceCollection ConnectImplementingType(this IServiceCollection services, Type interfaceType)
        {

            string name = interfaceType.Name;
            var allTypes = _types.Values.SelectMany(x => x);
            foreach (var item in allTypes)
            {
                var interfaces = item.GetInterfaces();
                if (item.IsAbstract || !item.IsClass) continue;

                if (interfaces != null && interfaces.Any(t => t.Name == name))
                {
                    services.AddScoped(item);
                    foreach (var t in interfaces.Where(t => t.Name == name))
                    {
                        services.AddScoped(t, s => s.GetRequiredService(item));
                    }
                }
            }
            return services;
        }

        public static IServiceCollection AddEventStore<TRepository, TTransaction, TDomainType, TReadModelType>(this IServiceCollection services)
            where TRepository : IAsyncRepository
            where TTransaction : ITransaction
        {
            return services.AddEventStore<TRepository, TTransaction>(opts =>
             {
                 opts.DomainAssemblyTypes = new List<Type>() { typeof(TDomainType) };
                 opts.ReadModelAssemblyTypes = new List<Type>() { typeof(TReadModelType) };
             });
        }

        public static IServiceCollection AddEventStore<TRepository, TTransaction>(this IServiceCollection services, Action<EventStoreOptions> options)
        where TRepository : IAsyncRepository
        where TTransaction : ITransaction
        {
            var opts = new EventStoreOptions();
            options(opts);

            var lookup = new EventTypeLookupStrategy();
            foreach (var t in opts.DomainAssemblyTypes)
            {
                lookup.ScanAssemblyOfType(t);
            }


            services
                .RegisterTypesFrom(opts.DomainAssemblyTypes)
                .ConnectImplementingType(typeof(IProcessCommand<>))
                .ConnectImplementingType(typeof(IHandleStatelessEvent<>))
                .ConnectImplementingType(typeof(IHandleAggregateCommandMarker<>))
                .ConnectImplementingType(typeof(INeedExternalValidation<>));

            services.RegisterTypesFrom(opts.ReadModelAssemblyTypes)
                .ConnectImplementingType(typeof(IProject<>))
                .ConnectImplementingType(typeof(IProjectAsync<>));
                .ConnectImplementingType(typeof(IProjectWithResult<>));
                .ConnectImplementingType(typeof(IProjectWithResultAsync<>));


            services.AddScoped<IUser>(opts.GetUserContext);
            services.AddScoped(typeof(ITransaction), typeof(TTransaction));
            services.AddScoped<ICommandContext>(s => new CommandContext(s.GetRequiredService<IUser>(), s.GetRequiredService<ITransaction>(), null, "1.0"));

            services.AddScoped<IAsyncCommandProcessor, AsyncPipelineProcessor>();
            services.AddScoped<ISecurityContext, SecurityContext>();
            services.AddScoped(typeof(IAsyncRepository), typeof(TRepository));
            services.AddSingleton<IEventTypeLookupStrategy>(lookup);
            services.AddScoped<IFactory, ServiceProviderFactory>();



            return services;
        }
    }

    public class EventStoreOptions
    {
        public IEnumerable<Type> DomainAssemblyTypes { get; set; }
        public IEnumerable<Type> ReadModelAssemblyTypes { get; set; }
        public Func<IServiceProvider, IUser> GetUserContext { get; set; }
    }
}
