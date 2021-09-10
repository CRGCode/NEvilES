using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NEvilES.Abstractions;
using Xunit;

namespace NEvilES.Testing
{
    public abstract class BaseCommandHandlerTest
    {
        private readonly Dictionary<Guid, Given> givenStreams = new Dictionary<Guid, Given>();
        protected IRepository TestingRepository;
        private readonly List<object> handlers;

        protected BaseCommandHandlerTest()
        {
            handlers = new List<object>();
            TestingRepository = new TestRepository(givenStreams);
        }

        protected void AddHandler(object handler)
        {
            handlers.Add(handler);
        }

        protected void ClearHandlers()
        {
            handlers.Clear();
        }

        public void Test<TAggregate>(Given<TAggregate> giv, Action<TAggregate> handler, Dictionary<Guid, IEnumerable<IMessage>> then)
            where TAggregate : AggregateBase
        {
            givenStreams[giv.StreamId] = giv;

            if (handler == null)
            {
                var aggregateType = typeof(TAggregate);
                throw new Exception($"No handler found on aggregate '{aggregateType.FullName}'");
            }

            var agg = TestingRepository.Get<TAggregate>(giv.StreamId);
            handler(agg);

            if (!then.Any())
            {
                return;
            }

            var expectedEvents = then.SelectMany(x => x.Value);
            var receivedEvents = ((IAggregate)agg).GetUncommittedEvents();

            if (expectedEvents.Count() != receivedEvents.Count)
            {
                var resultString = new StringBuilder();
                resultString.AppendLine(
                    $"Expected {expectedEvents.Count()} events, but received {receivedEvents.Count}");

                resultString.AppendLine();
                resultString.AppendLine("\tExpected:");
                foreach (var expected in expectedEvents)
                {
                    resultString.AppendLine($"\t\t - {expected.GetType()}");
                }

                resultString.AppendLine();
                resultString.AppendLine("\tReceived:");
                foreach (var result in receivedEvents)
                {
                    resultString.AppendLine($"\t\t - {result.GetType()}");
                }

                throw new Exception(resultString.ToString());
            }

            foreach (var expected in then)
            {
                TestExpectedEvents(expected.Key, expected.Value, receivedEvents.Cast<IEventData>());
            }
        }

        public void TestForException<TAggregate>(Given<TAggregate> giv, Action<TAggregate> handler, string messageContains = null)
            where TAggregate : AggregateBase
        {
            givenStreams[giv.StreamId] = giv;

            if (handler == null)
            {
                var aggregateType = typeof(TAggregate);
                throw new Exception($"No handler found on aggregate '{aggregateType.FullName}'");
            }

            var agg = TestingRepository.Get<TAggregate>(giv.StreamId);
            var exception = Record.Exception(() => handler(agg));

            Assert.True(exception != null, "Exception was expected. Command has passed where it should have failed");
            Assert.True(exception is DomainAggregateException,
                $"Domain Exception was expected! However this was thrown - {exception}");
            Assert.True(messageContains == null || exception.Message.Contains(messageContains));
            Console.WriteLine(exception.Message);
        }

        protected Dictionary<Guid, IEnumerable<IMessage>> Expect(Guid streamId, params IMessage[] messages)
        {
            return new Dictionary<Guid, IEnumerable<IMessage>>
            {
                {streamId, messages}
            };
        }

        private static void TestExpectedEvents(Guid id, IEnumerable<IMessage> events, IEnumerable<IEventData> actual)
        {
            var b = new StringBuilder();
            var check = actual.Select(x => new Result(false, x.Event)).ToArray();
            foreach (var c in check) { b.AppendLine($"\t{c.Value.GetType()}"); }

            foreach (var e in events)
            {
                Assert.True(check.Any(r =>
                {
                    if (r.Tested)
                        return false;
                    var expectedType = e.GetType();
                    var resultType = r.Value.GetType();
                    if (expectedType != resultType)
                        return false;
                    if (!IdMatch(id, r.Value))
                        return false;
                    r.Tested = true;
                    return CompareEvents(e, r.Value);
                }), $"{e.GetType()} Event not found within aggregate results. Events received:\r\n{b.ToString()}");
            }
        }

        private static bool IdMatch(Guid id, object value)
        {
            var type = value.GetType().GetTypeInfo();
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.PropertyType == typeof(Guid))
                .Select(pa => pa.GetValue(value))
                .Any(valExpected => id == (Guid)valExpected);
        }

        private class Result
        {
            public bool Tested { get; set; }
            public object Value { get; }

            public Result(bool tested, object value)
            {
                Tested = tested;
                Value = value;
            }
        }

        private static bool CompareEvents(object expected, object result)
        {
            var expectedType = expected.GetType().GetTypeInfo();
            var resultType = result.GetType().GetTypeInfo();
            foreach (var pa in expectedType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead))
            {
                var valResult = resultType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .First(p => p.CanRead && p.Name == pa.Name)
                                             .GetValue(result);
                var valExpected = pa.GetValue(expected);
                if (valExpected == null || (pa.PropertyType == typeof(Guid) && valExpected.Equals(Guid.Empty)))
                    continue;
                Assert.True(valResult != null,
                    $"EventType {expectedType.Name} has a Property '{pa.Name}' that was null, when it should have been '{valExpected}'. \nLook at your command handler and make sure your command passes thru all command properties to the event being raised");

                Assert.True(valExpected.CompareEx(valResult), $"Property {pa.Name} doesn't match expected value. EventType {expectedType.Name}. Expected '{valExpected}', but was '{valResult}'");
            }
            return true;
        }
    }

    public static class ObjectExtensions
    {
        public static bool DeepCompare(this object obj, object another)
        {
            if (ReferenceEquals(obj, another)) return true;
            if ((obj == null) || (another == null)) return false;
            //Compare two object's class, return false if they are difference
            if (obj.GetType() != another.GetType()) return false;

            var result = true;
            //Get all properties of obj
            //And compare each other
            foreach (var property in obj.GetType().GetTypeInfo().GetProperties())
            {
                var objValue = property.GetValue(obj);
                var anotherValue = property.GetValue(another);
                if (!objValue.Equals(anotherValue)) result = false;
            }

            return result;
        }

        public static bool CompareEx(this object obj, object another)
        {
            if (ReferenceEquals(obj, another)) return true;
            if ((obj == null) || (another == null)) return false;
            if (obj.GetType() != another.GetType()) return false;

            //properties: int, double, DateTime, etc, not class
            if (!obj.GetType().GetTypeInfo().IsClass) return obj.Equals(another);

            var result = true;
            foreach (var property in obj.GetType().GetTypeInfo().GetProperties())
            {
                var objValue = property.GetValue(obj);
                var anotherValue = property.GetValue(another);
                //Recursion
                if (!objValue.DeepCompare(anotherValue)) result = false;
            }
            return result;
        }
    }
}
