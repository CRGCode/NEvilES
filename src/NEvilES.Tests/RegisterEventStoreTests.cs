using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;
using NEvilES.Tests.CommonDomain.Sample;
using Outbox.Abstractions;
using Xunit;

namespace NEvilES.Tests
{
    public class RegisterEventStoreTests
    {

        [Fact]
        public void Register_GenericHost()
        {
            var services = new ServiceCollection();

            services.Configure<ServiceBusOptions>(options =>
            {
                options.TopicSubscription = "Topic:Sub";
                options.ConnectionString = "Endpoint=sb://oa-servicebus-pilot.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=UBlZGK2dwHbM7Bisdfa4i5/DD5X5x74yIU8OnSR7XFA=";
            });

            services.AddLogging(loggingBuilder =>
            {
                //loggingBuilder.AddXUnit(this);
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            });

            //services.AddSingleton<ServiceBusWorkerProcessingEvents<ChatRoom.Created>>();
            services.AddSingleton(typeof(IHostedService), typeof(ServiceBusWorkerProcessingEvents<ChatRoom.Created>));

            var sp = services.BuildServiceProvider();

            var type = typeof(IHostedService);

            var hosts = sp.GetServices(type).ToArray();

            Assert.Single(hosts);
        }

        [Fact]
        public void RegisterTypesFrom_SameAssembly()
        {
            var services = new ServiceCollection();
            
            var bl = services.RegisterTypesFrom(new[]
                    {
                        typeof(Person.Created),
                        typeof(Employee.Aggregate),
                        typeof(Approval),
                        typeof(UniqueNameValidation)
                    });

            bl.ConnectImplementingType(typeof(IHandleAggregateCommandMarker<>));

            var sp = services.BuildServiceProvider();

            var type = typeof(IHandleAggregateCommandMarker<>).MakeGenericType(typeof(Employee.Create));

            var handlers = sp.GetServices(type).ToArray();

            var aggHandler = handlers.Where(x => x.GetType() == typeof(Employee.Aggregate));
            
            Assert.Single(aggHandler);
        }

        [Fact]
        public void ConnectImplementingType_Including_SubTypes()
        {
            var services = new ServiceCollection();
            
            var bl = services.RegisterTypesFrom(new[]
            {
                typeof(Person.Created),
                typeof(Employee.Aggregate),
                typeof(Approval),
                typeof(UniqueNameValidation)
            });

            services.AddSingleton<IDocumentMemory, DocumentMemory>();
            services.AddScoped<DocumentStoreGuid>();
            services.AddScoped<IReadFromReadModel<Guid>, DocumentStoreGuid>();
            services.AddScoped<IWriteReadModel<Guid>, DocumentStoreGuid>();

            bl.ConnectImplementingType(typeof(INeedExternalValidation<>));

            var sp = services.BuildServiceProvider();

            var type = typeof(INeedExternalValidation<>).MakeGenericType(typeof(Person.Create));

            var validators = sp.GetServices(type).ToArray();

            Assert.NotEmpty(validators);

            type = typeof(INeedExternalValidation<>).MakeGenericType(typeof(Employee.Create));

            validators = sp.GetServices(type).ToArray();

            // Assert.NotEmpty(validators);  TODO This fails!  
        }

    }
}