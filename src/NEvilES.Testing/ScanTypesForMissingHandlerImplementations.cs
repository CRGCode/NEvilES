using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.Testing
{
    public abstract class ScanTypesForMissingHandlerImplementations<TCommand, TAggregate, TEvent, TProjector>
    {
        private readonly ITestOutputHelper testOutputHelper;

        protected ScanTypesForMissingHandlerImplementations(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void FindCommandsWithoutHandlers()
        {
            var missing = FindMissing(typeof(ICommand), typeof(IEvent), typeof(TCommand).Assembly, 
                typeof(IHandleAggregateCommandMarker<>), typeof(TAggregate).Assembly);

            Assert.Empty(missing);
        }

        //[RunnableInDebugOnly]
        [Fact]
        public void FindEventsWithoutProjectors()
        {
            var missing = FindMissing(typeof(IEvent), null, typeof(TEvent).Assembly,
                typeof(IProject<>), typeof(TProjector).Assembly);

            Assert.Empty(missing);
        }

        private List<TypeInfo> FindMissing(Type include, Type exclude, Assembly assemblyWithInterfaces, Type openType, Assembly assemblyWithHandlers)
        {
            var types = assemblyWithInterfaces.DefinedTypes
                .Where(t =>
                {
                    if (t.IsAbstract) return false;
                    var interfaces = t.GetInterfaces();
                    var found = false;
                    foreach (var i in interfaces)
                    {
                        if (i == exclude)
                            return false;
                        if (i == include)
                        {
                            found = true;
                        }
                    }

                    return found;
                }).ToArray();

            var handlers = assemblyWithHandlers.DefinedTypes
                .Where(a => a.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openType)).ToArray();

            var missing = new List<TypeInfo>();
            foreach (var c in types)
            {
                var cnt = handlers.Count(h => h.GetInterfaces()
                    .Any(i => i.GenericTypeArguments.Any(t => t == c)));

                if (cnt == 0)
                {
                    missing.Add(c);
                }
            }
            testOutputHelper.WriteLine($"{assemblyWithInterfaces.GetName().Name} Types - {types.Count()}");
            testOutputHelper.WriteLine($"{assemblyWithHandlers.GetName().Name} Handlers - {handlers.Count(a => a.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openType))}");

            if (!missing.Any())
            {
                return missing;
            }

            testOutputHelper.WriteLine($"\nAll of the following are not Handled!\nTotal unhandled {missing.Count}");
            foreach (var typeInfo in missing)
            {
                testOutputHelper.WriteLine($"{typeInfo.FullName}");
            }
            return missing;
        }
    }
}