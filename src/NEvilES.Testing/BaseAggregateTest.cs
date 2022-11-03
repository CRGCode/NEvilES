using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NEvilES.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace NEvilES.Testing
{
    public abstract class BaseAggregateTest<TAggregate>
        where TAggregate : AggregateBase, IAggregate, new()
    {
        private readonly TAggregate sut;
        protected object Handler;

        protected BaseAggregateTest()
        {
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
                return agg.GetUncommittedEvents().Cast<EventData>().Select(x => x.Event).ToArray();
            };
        }

        protected Action<object> Then(params object[] expectedEvents)
        {
            return got =>
            {
                switch (got)
                {
                    case Exception exception:
                        throw exception;
                    case object[] gotEvents when gotEvents.Length == expectedEvents.Length:
                    {
                        for (var i = 0; i < gotEvents.Length; i++)
                        {
                            var expectedType = expectedEvents[i].GetType();
                            var actualType = gotEvents[i].GetType();
                            Assert.True(expectedType == actualType || actualType.GetTypeInfo().IsSubclassOf(expectedType),
                                $"Incorrect event in results; expected a {expectedType.Name} but got a {actualType.Name}");
                            Assert.Equal(JsonConvert.SerializeObject(expectedEvents[i]), JsonConvert.SerializeObject(gotEvents[i]));
                        }

                        break;
                    }
                    case object[] gotEvents when gotEvents.Length > expectedEvents.Length:
                    {
                        var diff = string.Join(", ", EventDiff(gotEvents, expectedEvents));
                        Assert.True(false, $"Expected event(s) missing: {diff}");
                        break;
                    }
                    case object[] gotEvents:
                    {
                        var diff = string.Join(", ", EventDiff(expectedEvents, gotEvents));
                        Assert.True(false, $"Unexpected event(s) emitted: {diff}");
                        break;
                    }
                    default:
                        Assert.True(false, $"Expected events, but got exception {got.GetType().Name}");
                        break;
                }
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
                    throw (Exception) got;

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
    }

    public class ThenFailWithConditionFailed : Exception
    {
        public ThenFailWithConditionFailed(string condition, Exception innerException)
            : base($@"Then failed with ""{condition}""", innerException)
        {
        }
    }
}