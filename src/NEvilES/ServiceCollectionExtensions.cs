using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;

namespace NEvilES
{
    public static class ServiceCollectionExtensions
    {
        public static Dictionary<Assembly, Type[]> Types = new Dictionary<Assembly, Type[]>();
        public interface IRegisteredTypesBuilder
        {
            IRegisteredTypesBuilder ConnectImplementingType(Type interfaceType);
        }

        public class RegisteredTypesBuilder : IRegisteredTypesBuilder
        {
            private readonly IEnumerable<Type> assemblyTypes;
            private IServiceCollection services;
            public RegisteredTypesBuilder(IEnumerable<Type> assemblyTypes, IServiceCollection services)
            {
                this.assemblyTypes = assemblyTypes;
                this.services = services;
            }

            public IRegisteredTypesBuilder ConnectImplementingType(Type interfaceType)
            {
                var name = interfaceType.Name;
                foreach (var item in assemblyTypes)
                {
                    if (item.IsAbstract || !item.IsClass) 
                        continue;
                    var interfaces = item.GetInterfaces().ToArray();

                    if(!interfaces.Any())
                        continue;

                    var matchingInterfaces = interfaces.Where(t => t.Name == name).ToArray();

                    if (!matchingInterfaces.Any()) 
                        continue;

                    services = ConnectImplementingTypes(services, item, matchingInterfaces);
                }

                return this;
            }

            public IServiceCollection Build() => services;

            private static IServiceCollection ConnectImplementingTypes(IServiceCollection services, Type implementingType, IEnumerable<Type> interfaceTypes)
            {
                services.AddScoped(implementingType);
                foreach (var t in interfaceTypes)
                {
                    services.AddScoped(t, s => s.GetRequiredService(implementingType));
                }
                return services;
            }
        }

        //public static IRegisteredTypesBuilder RegisterTypesFrom(this IServiceCollection services, params Type[] assemblyType)
        //    => RegisterTypesFrom(services, assemblyType);

        public static IRegisteredTypesBuilder RegisterTypesFrom(this IServiceCollection services, IEnumerable<Type> assemblyType)
        {
            var assemblies = new HashSet<Assembly>();
            lock (Types)
            {
                foreach (var type in assemblyType)
                {
                    if (!Types.ContainsKey(type.Assembly))
                    {
                        Types.Add(type.Assembly, type.Assembly.GetTypes());
                    }

                    if (!assemblies.Contains(type.Assembly))
                    {
                        assemblies.Add(type.Assembly);
                    }
                }
            }

            var assemblyTypes = assemblies.SelectMany(x => Types[x]).ToArray();
            return new RegisteredTypesBuilder(assemblyTypes, services);
        }

        public static IServiceCollection ConnectImplementingType(this IServiceCollection services, Type interfaceType)
        {
            var name = interfaceType.Name;
            var allTypes = Types.Values.SelectMany(x => x);
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

        public static IServiceCollection AddEventStoreAsync<TRepository, TTransaction, TDomainType, TReadModelType>(this IServiceCollection services)
            where TRepository : IAsyncRepository
            where TTransaction : ITransaction
        {
            return services.AddEventStoreAsync<TRepository, TTransaction>(opts =>
             {
                 opts.DomainAssemblyTypes = new List<Type>() { typeof(TDomainType) };
                 opts.ReadModelAssemblyTypes = new List<Type>() { typeof(TReadModelType) };
             });
        }

        public static IServiceCollection AddEventStoreAsync<TRepository, TTransaction>(this IServiceCollection services, Action<EventStoreOptions> options)
            where TRepository : IAsyncRepository
            where TTransaction : ITransaction
        {
            var opts = new EventStoreOptions();
            options(opts);

            foreach (var pipelineStage in opts.PipelineStages)
            {
                PipelineProcessor.AddStage(pipelineStage);
            }

            var lookup = new EventTypeLookupStrategy();
            foreach (var t in opts.DomainAssemblyTypes)
            {
                lookup.ScanAssemblyOfType(t);
            }

            services
                .RegisterTypesFrom(opts.DomainAssemblyTypes)
                .ConnectImplementingType(typeof(IHandleCommand<>))
                .ConnectImplementingType(typeof(IHandleStatelessEvent<>))
                .ConnectImplementingType(typeof(IHandleAggregateCommandMarker<>))
                .ConnectImplementingType(typeof(INeedExternalValidation<>));

            services.RegisterTypesFrom(opts.ReadModelAssemblyTypes)
                .ConnectImplementingType(typeof(IProject<>))
                .ConnectImplementingType(typeof(IProjectAsync<>))
                .ConnectImplementingType(typeof(IProjectWithResult<>))
                .ConnectImplementingType(typeof(IProjectWithResultAsync<>));

            services.AddScoped(opts.GetUserContext);
            services.AddScoped(typeof(ITransaction), typeof(TTransaction));
            services.AddScoped<ICommandContext>(s =>
                new CommandContext(s.GetRequiredService<IUser>(), s.GetRequiredService<ITransaction>(), null, "1.0"));
 
            services.AddScoped<PipelineProcessor>();
            services.AddScoped(s => s.GetRequiredService<ICommandContext>().Result);
            services.AddScoped<ISecurityContext, SecurityContext>();

            services.AddScoped(typeof(IAsyncRepository), typeof(TRepository));
            services.AddSingleton<IEventTypeLookupStrategy>(lookup);
            services.AddScoped<IFactory, ServiceProviderFactory>();

            return services;
        }

        public static IServiceCollection AddEventStore<TRepository, TTransaction>(this IServiceCollection services, Action<EventStoreOptions> options)
            where TRepository : IRepository
            where TTransaction : ITransaction
        {
            var opts = new EventStoreOptions();
            options(opts);

            foreach (var pipelineStage in opts.PipelineStages)
            {
                PipelineProcessor.AddStage(pipelineStage);
            }

            var lookup = new EventTypeLookupStrategy();
            foreach (var t in opts.DomainAssemblyTypes)
            {
                lookup.ScanAssemblyOfType(t);
            }

            services
                .RegisterTypesFrom(opts.DomainAssemblyTypes)
                .ConnectImplementingType(typeof(IHandleCommand<>))
                .ConnectImplementingType(typeof(IProcessCommandAsync<>))
                .ConnectImplementingType(typeof(IHandleStatelessEvent<>))
                .ConnectImplementingType(typeof(IHandleAggregateCommandMarker<>))
                .ConnectImplementingType(typeof(INeedExternalValidation<>));

            services.RegisterTypesFrom(opts.ReadModelAssemblyTypes)
                .ConnectImplementingType(typeof(IProject<>))
                .ConnectImplementingType(typeof(IProjectAsync<>))
                .ConnectImplementingType(typeof(IProjectWithResult<>))
                .ConnectImplementingType(typeof(IProjectWithResultAsync<>));

            services.AddScoped(typeof(ITransaction), typeof(TTransaction));
            services.AddScoped(typeof(TRepository));

            services.AddTransient<ICommandProcessor, PipelineProcessorWithScopedRetry>();

            services.AddScoped<PipelineProcessor>();
            services.AddScoped(s => s.GetRequiredService<ICommandContext>().Result);
            services.AddScoped<ISecurityContext, SecurityContext>();

            services.AddScoped(typeof(IAsyncRepository), typeof(TRepository));
            services.AddScoped(typeof(IRepository), typeof(TRepository));
            services.AddScoped(typeof(IReadEventStore), typeof(TRepository));
            services.AddSingleton<IEventTypeLookupStrategy>(lookup);
            services.AddScoped<IFactory, ServiceProviderFactory>();

            return services;
        }

        public static IServiceCollection AddEventStoreReader<TReader>(this IServiceCollection services, Type[] aggregateTypes) 
            where TReader : IReadEventStore
        {
            var lookup = new EventTypeLookupStrategy();
            foreach (var t in aggregateTypes)
            {
                lookup.ScanAssemblyOfType(t);
            }

            //services.RegisterTypesFrom(aggregateTypes);

            services.AddScoped(typeof(IReadEventStore), typeof(TReader));
            services.AddSingleton<IEventTypeLookupStrategy>(lookup);
            services.AddScoped<IFactory, ServiceProviderFactory>();

            return services;
        }

        public static IServiceCollection AddAllGenericTypes(this IServiceCollection services, Type genericType, Assembly[] assemblies)
        {
            var typesFromAssemblies = assemblies.SelectMany(a => a.DefinedTypes.Where(x => x.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType))).ToArray();

            foreach (var item in typesFromAssemblies)
            {
                var interfaces = item.GetInterfaces();
                if (item.IsAbstract || !item.IsClass)
                    continue;

                var name = genericType.Name;
                if (interfaces.All(type => type.Name != name))
                    continue;
                services.AddScoped(item);
                foreach (var type in interfaces.Where(x => x.Name == name))
                {
                    services.AddScoped(type, s => s.GetRequiredService(item));
                }
            }

            return services;
        }
    }

    public class EventStoreOptions
    {
        public IEnumerable<Type> DomainAssemblyTypes { get; set; }
        public IEnumerable<Type> ReadModelAssemblyTypes { get; set; }
        public Func<IServiceProvider, IUser> GetUserContext { get; set; }
        public IEnumerable<Type> PipelineStages { get; set; }
    }
}
