using System;
using System.Data;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NEvilES.Abstractions;
using NEvilES.Abstractions.Pipeline;
using NEvilES.Pipeline;
using NEvilES.Tests.CommonDomain.Sample;
using Xunit;
using Xunit.Abstractions;
using MartinCostello.Logging.XUnit;

namespace NEvilES.Tests;

public class InMemoryPipelineProcessorTests : IClassFixture<InMemoryFixtureContext>, IDisposable
{
    private readonly ICommandProcessor commandProcessor;
    private readonly IRepository repository;
    private readonly IServiceScope scope;

    public InMemoryPipelineProcessorTests(InMemoryFixtureContext context, ITestOutputHelper output)
    {
        context.OutputHelper = output;
        scope = context.Container.CreateScope();
        commandProcessor = scope.ServiceProvider.GetRequiredService<ICommandProcessor>();
        repository = scope.ServiceProvider.GetRequiredService<IRepository>();
    }

    [Fact]
    public void CommandWithSeparateHandler()
    {
        var streamId = Guid.NewGuid();

        var expected =
            commandProcessor.Process(new Customer.SendInvite(streamId,
                new PersonalDetails("John", $"Smith{streamId}"), ""));

        Assert.Equal(streamId, expected.FilterEvents<Customer.Created>().First().CustomerId);

        expected =
            commandProcessor.Process(new Customer.Complain(){ CustomerId = streamId, Reason = "Some Blah"});
    }

    [Fact]
    public void CommandWithDifferentEventHandlerOnAggregate()
    {
        var streamId = Guid.NewGuid();

        var expected = commandProcessor.Process(new Employee.Create
            { PersonId = streamId, Person = new PersonalDetails("John", $"Smith{streamId}") });
        Assert.Equal(streamId, expected.FilterEvents<Person.Created>().First().PersonId);
    }

    [Fact]
    public void CommandWithDifferentEventHandlerOnAggregateWithException()
    {
        var streamId = Guid.NewGuid();

        Assert.Throws<DomainAggregateException>(() =>
            commandProcessor.Process(new Employee.Create
                { PersonId = streamId, Person = new PersonalDetails("John", "God") }));
    }

    [Fact]
    public void CommandWithHandlerDependencies()
    {
        var streamId = Guid.NewGuid();

        var netAmount = 60000M;
        commandProcessor.Process(new Employee.Create
            { PersonId = streamId, Person = new PersonalDetails("John", $"Smith{streamId}") });
        var expected = commandProcessor.Process(new Employee.PayPerson
            { EmployeeId = streamId, NetAmount = netAmount });
        var payPerson = expected.FilterEvents<Employee.PaidPerson>().First();
        Assert.Equal(streamId, payPerson.EmployeeId);
        Assert.True(payPerson.Tax < netAmount);
    }

    [Fact]
    public void ProcessStatelessEvent()
    {
        var streamId = Guid.NewGuid();

        commandProcessor.Process(new Employee.Create { PersonId = streamId, Person = new PersonalDetails("John", "Smith") });

        var expected = commandProcessor.Process(new Person.StatelessBirthdateChanged { PersonId = streamId, Birthdate = DateTime.Now });
        Assert.Equal(streamId, expected.FilterEvents<Person.StatelessBirthdateChanged>().First().PersonId);
    }

    public void Dispose()
    {
        scope?.Dispose();
    }
}

public class InMemoryFixtureContext : ITestOutputHelperAccessor
{
    public IServiceProvider Container { get; }
    public ITestOutputHelper OutputHelper { get; set; }

    public InMemoryFixtureContext()
    {
        var services = new ServiceCollection()
            .AddScoped(c =>
            {
                var conn = c.GetRequiredService<IDbConnection>();
                return conn.BeginTransaction();
            })
            .AddEventStore<InMemoryEventStore, Transaction>(opts =>
            {
                opts.PipelineStages = new[]
                {
                    typeof(ValidationPipelineProcessor),
                    typeof(CommandPipelineProcessor),
                    typeof(ReadModelPipelineProcess)
                };

                opts.DomainAssemblyTypes = new[]
                {
                    typeof(Person.Created),
                    typeof(Approval),
                    typeof(UniqueNameValidation)
                };

                opts.GetUserContext = s => new CommandContext.User(CombGuid.NewGuid());

                opts.ReadModelAssemblyTypes = new[]
                {
                    typeof(Person.Created),
                };
            });

        services.AddLogging(configure => configure.AddXUnit(this).SetMinimumLevel(LogLevel.Trace));

        services.AddSingleton<IUser>(c => new CommandContext.User(Guid.Parse("00000001-0007-4852-9D2D-111111111111")));
        services.AddScoped<ICommandContext, CommandContext>(s =>
        {
            var user = s.GetRequiredService<IUser>();
            var transaction = s.GetRequiredService<ITransaction>();
            return new CommandContext(user, transaction, null, "1.0");
        });

        services.AddScoped<IFactory, ServiceProviderFactory>();
        services.AddScoped<IReadEventStore, InMemoryEventStore>();
        services.AddSingleton<IDocumentMemory, DocumentMemory>();
        services.AddScoped<DocumentStoreGuid>();
        services.AddScoped<IReadFromReadModel<Guid>>(s => s.GetRequiredService<DocumentStoreGuid>());
        services.AddScoped<IWriteReadModel<Guid>>(s => s.GetRequiredService<DocumentStoreGuid>());
        services.AddScoped<DocumentStoreString>();
        services.AddScoped<IReadFromReadModel<string>>(s => s.GetRequiredService<DocumentStoreString>());
        services.AddScoped<IWriteReadModel<string>>(s => s.GetRequiredService<DocumentStoreString>());

        services.AddScoped<TaxRuleEngine>();
        services.AddScoped<IApprovalWorkflowEngine, ApprovalWorkflowEngine>();

        Container = services.BuildServiceProvider();
    }
}