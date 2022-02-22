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
    public class MissingHandlers
    {
        private readonly ITestOutputHelper testOutputHelper;

        public MissingHandlers(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        public  void  FindCommandsWithoutHandlers(Assembly assemblyWithCommands, Assembly assemblyWithAggregates)
        {
            var missing = FindMissing(typeof(ICommand), typeof(IEvent), assemblyWithCommands, 
                typeof(IHandleAggregateCommandMarker<>), assemblyWithAggregates);

            Assert.Empty(missing);
        }

        //[RunnableInDebugOnly]
        public void FindEventsWithoutProjectors(Assembly assemblyWithEvents, Assembly assemblyWithProjectors)
        {
            var missing = FindMissing(typeof(IEvent), null, assemblyWithEvents,
                typeof(IProject<>), assemblyWithProjectors);

            Assert.Empty(missing);
        }

        private List<TypeInfo> FindMissing(Type include, Type exclude, Assembly assemblyWithInterfaces, Type openType, Assembly assemblyWithHandlers)
        {
            var types = assemblyWithInterfaces.DefinedTypes
                .Where(t =>
                {
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
                var cnt = handlers.Count(prj => prj.GetInterfaces()
                    .Any(i => i.GenericTypeArguments.Any(t => t == c)));

                if (cnt == 0)
                {
                    missing.Add(c);
                }
            }

            if (missing.Any())
            {
                testOutputHelper.WriteLine($"Total missing - {missing.Count}");
                foreach (var typeInfo in missing)
                {
                    testOutputHelper.WriteLine($"{typeInfo.FullName}");
                }
            }
            return missing;
        }
    }
}