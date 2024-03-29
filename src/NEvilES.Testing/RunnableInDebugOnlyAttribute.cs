using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.Testing
{
    public sealed class RunnableInDebugOnlyAttribute : FactAttribute
    {
        public RunnableInDebugOnlyAttribute()
        {
            if (!Debugger.IsAttached)
            {
                Skip = "Only running in interactive mode.";
            }
        }
    }

    public class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper testOutputHelper;

        public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
            => new XunitLogger(testOutputHelper, categoryName);

        public void Dispose()
        { }
    }

    public class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper testOutputHelper;
        private readonly string categoryName;

        public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
        {
            this.testOutputHelper = testOutputHelper;
            this.categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
            => NoopDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            testOutputHelper.WriteLine($"{categoryName} [{eventId}] {formatter(state, exception)}");
            if (exception != null)
                testOutputHelper.WriteLine(exception.ToString());
        }

        private class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new NoopDisposable();
            public void Dispose()
            { }
        }
    }
}