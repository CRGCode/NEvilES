using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NEvilES.Abstractions;

namespace NEvilES.Testing
{
    public class EventDeserializationIssuesTest
    {
        public void EventDeserializationIssues(Assembly assembly)
        {
            var eventInterface = typeof(IEvent);
            var assemblyUnderTest = assembly;

            var availableTypes = assemblyUnderTest.GetTypes().Select(x => x.GetTypeInfo()).Where(x => !x.IsAbstract && x.IsClass).ToList();

            var availableEvents = availableTypes
                .Where(x => x.GetInterfaces().Contains(eventInterface))
                .OrderBy(x => x.FullName)
                .ToList();

            var eventConstructionIssues = new List<string>();

            availableEvents.ForEach(@event =>
            {
                var largestConstructor = @event.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderByDescending(x => x.GetParameters().Length).FirstOrDefault();

                if (largestConstructor == null)
                    return;

                var parameters = largestConstructor.GetParameters();
                var props = @event.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var param in parameters)
                {
                    if (props.All(x => !string.Equals(x.Name, param.Name, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        eventConstructionIssues.Add($"Event {@event.FullName} is missing a matching property name for constructor field: {param.Name}");
                    }
                }

                if (!largestConstructor.IsPublic)
                {
                    eventConstructionIssues.Add($"Event {@event.FullName} 's largest constructor appears to be non public.");
                }
            });

            if (!eventConstructionIssues.Any())
                return;

            var r = new StringBuilder();
            r.AppendLine("Issues with these IEvent objects may cause deserialization problems:");
            eventConstructionIssues.ForEach(x => r.AppendLine(string.Format("\t- {0}", x)));

            throw new Exception(r.ToString());
        }
    }
}