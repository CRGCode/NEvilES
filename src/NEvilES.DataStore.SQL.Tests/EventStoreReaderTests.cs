using System;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions.Pipeline;
using Xunit;

namespace NEvilES.DataStore.SQL.Tests
{
    public class EventStoreReaderTests : IClassFixture<TestContext>
    {
        private readonly IReadEventStore reader;

        public EventStoreReaderTests(TestContext context)
        {
            reader = context.Services.GetRequiredService<IReadEventStore>();
        }

        [Fact]
        public void Read()
        {
            var events = reader.Read();

            foreach (var e in events)
            {
                var x = e.StreamId;
            }
        }

    }
}
