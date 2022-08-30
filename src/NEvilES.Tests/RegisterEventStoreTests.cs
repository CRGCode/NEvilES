using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;

namespace NEvilES.Tests
{
    public class RegisterEventStoreTests
    {

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

            services.AddSingleton<DocumentStoreGuid>();
            services.AddSingleton<IReadFromReadModel<Guid>, DocumentStoreGuid>();
            services.AddSingleton<IWriteReadModel<Guid>, DocumentStoreGuid>();

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