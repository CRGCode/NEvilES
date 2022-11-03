using NEvilES.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.DataStore.Marten.Tests
{
    [Collection("Integration")]
    public class MartenDocumentCreateTests : IClassFixture<TestContext>
    {
        private readonly TestContext context;
        private readonly ITestOutputHelper output;

        public MartenDocumentCreateTests(TestContext context, ITestOutputHelper output)
        {
            this.context = context;
            this.output = output;
        }


        [Fact]
        public void EventStoreCreate()
        {
            new PgSQLEventStoreCreate().CreateOrWipeDb(new ConnectionString(TestContext.ConnString));
        }
    }
}