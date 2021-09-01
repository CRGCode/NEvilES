using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Xunit;
using NEvilES.Pipeline;
using NEvilES.Abstractions;
using NEvilES.Tests.CommonDomain.Sample;

namespace NEvilES.Tests
{
    public class ApprovalWorkflowEngineTests : IClassFixture<SharedFixtureContext>, IDisposable
    {
        private static readonly Type ApproverType = typeof(ApprovalWorkflowEngine);
        private readonly IServiceScope scope;
        private readonly IServiceProvider container;
        private readonly IApprovalWorkflowEngine approvalWorkflowEngine;

        public ApprovalWorkflowEngineTests(SharedFixtureContext context)
        {
            scope = context.Container.CreateScope();
            container = scope.ServiceProvider;
            approvalWorkflowEngine = container.GetRequiredService<IApprovalWorkflowEngine>();
        }

        public object GetCommandUsingReflection(IApprovalWorkflowEngine approver, Approval.InnerCommand innerCommand)
        {
            var method = ApproverType.GetTypeInfo().GetMethod("UnwrapCommand");
            var genericMethod = method.MakeGenericMethod(innerCommand.Type);
            return genericMethod.Invoke(approver, new object[] { innerCommand });
        }

        private void MeasurePerformance(IApprovalWorkflowEngine approver, Approval.InnerCommand innerCommand, Func<IApprovalWorkflowEngine, Approval.InnerCommand, object> func)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var cmd = func(approver, innerCommand);
            stopwatch.Stop();
            Console.WriteLine("1st call : {0}ms", stopwatch.ElapsedMilliseconds);

            var loop = 20;
            Console.WriteLine("Running ({0}) calls....", loop);
            for (var i = 0; i < loop; i++)
            {
                stopwatch.Restart();
                var x = func(approver, innerCommand);
                stopwatch.Stop();
                Console.WriteLine("Call ({1}) : {0}us", stopwatch.ElapsedMicroSeconds(), i + 1);
            }

            stopwatch.Restart();
            loop = 1000000;
            for (var i = 0; i < loop; i++)
            {
                var x = func(approver, innerCommand);
            }
            stopwatch.Stop();
            Console.WriteLine("Many ({1}) calls : {0}us/call", (decimal)stopwatch.ElapsedMicroSeconds() / loop, loop);
        }

        [RunnableInDebugOnly]
        public void Approval_GetCommand_Performance_UsingReflection()
        {
            // Arrange
            var command = new Employee.Create { StreamId = Guid.NewGuid(), Person = new PersonalDetails("John", "Smith") };
            var result = approvalWorkflowEngine.Initiate(command);
            var repository = container.GetRequiredService<IRepository>();
            var approvalRequest = repository.Get<Approval.Aggregate>(result.UpdatedAggregates.First().StreamId);
            var innerCommand = approvalRequest.GetInnerCommand();

            // Act
            MeasurePerformance(approvalWorkflowEngine, innerCommand, GetCommandUsingReflection);
        }

        [RunnableInDebugOnly]
        public void Approval_GetCommand_Performance_UsingDynamic()
        {
            // Arrange
            var command = new Employee.Create { StreamId = Guid.NewGuid(), Person = new PersonalDetails("John", "Smith") };
            var result = approvalWorkflowEngine.Initiate(command);
            var repository = container.GetRequiredService<IRepository>();
            var approvalRequest = repository.Get<Approval.Aggregate>(result.UpdatedAggregates.First().StreamId);
            var innerCommand = approvalRequest.GetInnerCommand();

            // Act
            MeasurePerformance(approvalWorkflowEngine, innerCommand, (a, c) => ApprovalWorkflowEngine.GetCommand(c));
        }

        [Fact]
        public void ApprovalWorkflowEngine_Initiate()
        {
            var command = new Employee.Create { StreamId = Guid.NewGuid(), Person = new PersonalDetails("John", "Smith") };

            var expected = approvalWorkflowEngine.Initiate(command);

            Assert.Single(expected.UpdatedAggregates);
            Assert.Equal(command, expected.FilterEvents<Approval.Created>().First().InnerCommand.Command);
        }

        [Fact]
        public void ApprovalWorkflowEngine_Transition()
        {
            var command = new Employee.Create { StreamId = Guid.NewGuid(), Person = new PersonalDetails("John", "Smith") };
            var result = approvalWorkflowEngine.Initiate(command);
            var approvalRequest = result.FilterEvents<Approval.Created>().First();

            var expected = approvalWorkflowEngine.Transition(approvalRequest.StreamId, "blah");

            var stateChangedEvent = expected.FilterEvents<Approval.StateChanged>().First();
            Assert.Equal(approvalRequest.StreamId, stateChangedEvent.StreamId);
            Assert.Equal("blah", stateChangedEvent.State);
        }

        [Fact(Skip = "Worked with previous IOC but broken with .Net version - need to fix how we register the handlers")]
        public void ApprovalWorkflowEngine_Transition_Approve()
        {
            var command = new Employee.Create { StreamId = Guid.NewGuid(), Person = new PersonalDetails("John", "Smith") };
            var result = approvalWorkflowEngine.Initiate(command);
            var approvalRequest = result.FilterEvents<Approval.Created>().First();

            var expected = approvalWorkflowEngine.Transition(approvalRequest.StreamId, "Approved");

            Assert.Equal(command.Person.Name, expected.FilterEvents<Person.Created>().First().Person.Name);
        }

        [Fact]
        public void Json_Constructors()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Populate,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                Converters = new JsonConverter[] { new StringEnumConverter() }
            };

            var json = JsonConvert.SerializeObject(new Approval.Created { InnerCommand = new Approval.InnerCommand(typeof(Person.Create), new Employee.Create(), Guid.NewGuid()) }, serializerSettings);
            var message = JsonConvert.DeserializeObject<Approval.Created>(json, serializerSettings);
            Assert.NotNull(message.InnerCommand);
        }

        [Fact(Skip = "Worked with previous IOC but broken now....... look at the type registration")]
        public void ApprovalWorkflowEngine_Transition_Approve_RunComplexCommands()
        {
            // Arrange
            var streamId = Guid.NewGuid();

            var command = new Person.SendInvite(streamId, new PersonalDetails("John", "Smith"), "john@gmail.com");
            var result = approvalWorkflowEngine.Initiate(command);
            var approvalRequest = result.FilterEvents<Approval.Created>().First();

            // Act
            var expected = approvalWorkflowEngine.Transition(approvalRequest.StreamId, "Approved");

            var projectedItem = expected.FindProjectedItem<PersonalDetails>();
            Assert.True(projectedItem.FirstName == command.Person.FirstName);

            var person = expected.FilterEvents<Person.Created>().First();
            Assert.True(person.Person.LastName == command.Person.LastName);
            var email = expected.FilterEvents<Email.PersonInvited>().First();
            Assert.True(email.StreamId != streamId);
            Assert.True(email.EmailAddress == command.Email);
            Assert.Equal(4, expected.UpdatedAggregates.Count);
            Assert.Equal(approvalRequest.StreamId, expected.FilterEvents<Approval.StateChanged>().First().StreamId);
            Assert.Equal(command.Person.Name, expected.FilterEvents<Person.Created>().First().Person.Name);
        }

        public void Dispose()
        {
            scope?.Dispose();
        }
    }

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

    public static class StopwatchExtensions
    {
        public static long ElapsedNanoSeconds(this Stopwatch watch)
        {
            return watch.ElapsedTicks * 1000000000 / Stopwatch.Frequency;
        }
        public static long ElapsedMicroSeconds(this Stopwatch watch)
        {
            return watch.ElapsedTicks * 1000000 / Stopwatch.Frequency;
        }
    }
}