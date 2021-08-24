using System;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;

namespace NEvilES.DataStore.DynamoDB.Tests
{
    public class TestContext
    {

        public IServiceProvider Services { get;private set; }
        public TestContext()
        {


            IServiceCollection services = new ServiceCollection();


            services.AddEventStoreAsync<DynamoDBEventStore, DynamoDBTransaction>(opts =>
             {
                 opts.DomainAssemblyTypes = new[]
                 {
                    typeof(NEvilES.Tests.CommonDomain.Sample.Address),
                 };
                 opts.ReadModelAssemblyTypes = new[]
                 {
                    typeof(NEvilES.Tests.CommonDomain.Sample.Address),
                 };

                 opts.GetUserContext = (s => new Pipeline.CommandContext.User(CombGuid.NewGuid()));
             });

            var clientConfig = new AmazonDynamoDBConfig { ServiceURL = "http://localhost:8000" };
            var dynamoDbClient = new AmazonDynamoDBClient(clientConfig);
            services.AddScoped<IAmazonDynamoDB>(s => dynamoDbClient);



            Services = services.BuildServiceProvider();

            // var num1 = fS.GetService<IProjectAsync<TestAggregate.AddTransaction>>();
            // var num2 = fS.GetService<IProjectAsync<TestAggregate.AmountChanged>>();
            // var id = CombGuid.NewGuid();
            // var id = Guid.Parse("53a62c22-4dde-4854-a4ff-aa23018559fc");




        }
    }
}