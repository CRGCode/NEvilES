using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Serialization;
using Xunit;

namespace NEvilES.Testing
{
    public abstract class BaseAggregateTest<TAggregate>
        where TAggregate : AggregateBase, IAggregate, new()
    {
        private readonly TAggregate sut;
        protected object handler;
        private Dictionary<Type, object> aggregateCommandDependencies;

        protected BaseAggregateTest()
        {
            aggregateCommandDependencies = new Dictionary<Type, object>();
            sut = new TAggregate();
        }

        protected void Test(IEnumerable<IEvent> given, Func<TAggregate, object> when, Action<object> then)
        {
            object outcome = null;
            var ex = Record.Exception(() => outcome = when(ApplyEvents(sut, given)));
            then(outcome ?? ex);
        }

        protected IEnumerable<IEvent> Given(params IEvent[] events)
        {
            return events;
        }

        protected Func<TAggregate, object> When(Action<TAggregate> doAction)
        {
            return agg =>
            {
                doAction(agg);
                return agg.GetUncommittedEvents().Cast<EventData>().Select(x=>x.Event).ToArray();
            };
        }

        protected Action<object> Then(params object[] expectedEvents)
        {
            return got =>
            {
                if (got is Exception)
                {
                    throw (Exception)got;
                }

                var gotEvents = got as object[];
                if (gotEvents != null)
                {
                    if (gotEvents.Length == expectedEvents.Length)
                    {
                        for (var i = 0; i < gotEvents.Length; i++)
                        {
                            var expectedType = expectedEvents[i].GetType();
                            var actualType = gotEvents[i].GetType();
                            Assert.True(expectedType == actualType || actualType.GetTypeInfo().IsSubclassOf(expectedType), string.Format("Incorrect event in results; expected a {0} but got a {1}",
                                expectedType.Name, actualType.Name));
                            Assert.Equal(Serialize(expectedEvents[i]), Serialize(gotEvents[i]));
                        }
                    }
                    else
                    {
                        Assert.True(gotEvents.Length < expectedEvents.Length, string.Format("Expected event(s) missing: {0}",
                            string.Join(", ", EventDiff(expectedEvents, gotEvents))));
                        Assert.True(false, string.Format("Unexpected event(s) emitted: {0}",
                            string.Join(", ", EventDiff(gotEvents, expectedEvents))));
                    }
                }
                else
                    Assert.True(false, string.Format("Expected events, but got exception {0}",
                        got.GetType().Name));
            };
        }

        private static string[] EventDiff(object[] a, object[] b)
        {
            var diff = a.Select(e => e.GetType().Name).ToList();
            foreach (var remove in b.Select(e => e.GetType().Name))
                diff.Remove(remove);
            return diff.ToArray();
        }

        protected Action<object> ThenFailWith<TException>() where TException : Exception
        {
            return ThenFailWith<TException>(null);
        }

        protected Action<object> ThenFailWith<TException>(Expression<Func<TException, bool>> condition) where TException : Exception
        {
            return got =>
            {
                var ex = got as TException;
                if (ex == null)
                    throw (Exception)got;

                if (condition != null && !condition.Compile()(ex))
                {
                    var conditionString = condition.Body.ToString();

                    throw new ThenFailWithConditionFailed(conditionString, ex);
                }
            };
        }

        private TAggregate ApplyEvents(TAggregate agg, IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                agg.ApplyEvent(@event);
            }
            return agg;
        }

        private string Serialize(object obj)
        {
            //Daahhh Fuck?
            // var ser = new XmlSerializer(obj.GetType());
            // var ms = new MemoryStream();
            // ser.Serialize(ms, obj);
            // ms.Seek(0, SeekOrigin.Begin);
            // return new StreamReader(ms).ReadToEnd();
            return string.Empty;
        }
    }

    public class ThenFailWithConditionFailed : Exception
    {
        public ThenFailWithConditionFailed(string condition, Exception innerException)
            : base(string.Format(@"Then failed with ""{0}""", condition), innerException)
        { }
    }
}
