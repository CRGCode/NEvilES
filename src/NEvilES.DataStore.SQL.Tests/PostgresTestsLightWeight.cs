using System;
using System.Linq;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.DataStore.SQL.Tests;

[Collection("Serial")]
public class PostgresTestsLightWeight : IClassFixture<PostgresTestContextLightWeight>, IDisposable
{
    private readonly IServiceScope scope;

    public PostgresTestsLightWeight(PostgresTestContextLightWeight context, ITestOutputHelper output)
    {
        context.OutputHelper = output;
        var serviceScopeFactory = context.Container.GetRequiredService<IServiceScopeFactory>();
        scope = serviceScopeFactory.CreateScope();
    }

    [Fact]
    public void CommandRaises2Events()
    {
        var streamId = Guid.NewGuid();
        var commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
        var reader = scope.ServiceProvider.GetRequiredService<IReadFromReadModel<Guid>>();
        commandProcessor.Process(new Customer.Create() { CustomerId = streamId, Details = new PersonalDetails("John", "Smith") });

        const string reason = "Some reason for complaining";
        commandProcessor.Process(new Customer.Complain { CustomerId = streamId, Reason = reason });

        var customer = reader.Get<NEvilES.Tests.CommonDomain.Sample.ReadModel.Customer>(streamId);
        Assert.Equal(reason, customer.Notes.First());
        Assert.Empty(customer.Complaints);  // This is missing due to LightWeight session
    }

    public void Dispose()
    {
        scope?.Dispose();
    }
}

public class PostgresTestContextLightWeight : PostgresTestContext
{
    protected override void AddServices(IServiceCollection services)
    {
        base.AddServices(services);

        services.Replace(ServiceDescriptor.Scoped(s => s.GetRequiredService<IDocumentStore>().LightweightSession()));
    }
}
