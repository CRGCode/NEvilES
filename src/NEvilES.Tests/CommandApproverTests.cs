using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NEvilES.Pipeline;
using NEvilES.Tests.Sample;
using StructureMap;
using Xunit;
using Xunit.Abstractions;

namespace NEvilES.Tests
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

    public class CommandApproverTests : IClassFixture<SharedFixtureContext>
    {
        private readonly ICommandProcessor commandProcessor;
        private readonly IContainer container;
        private readonly CommandContext commandContext;
        private readonly ITestOutputHelper output;

        public CommandApproverTests(SharedFixtureContext context, ITestOutputHelper output)
        {
            container = context.Container.GetNestedContainer();
            commandProcessor = container.GetInstance<ICommandProcessor>();
            commandContext = container.GetInstance<CommandContext>();
            commandProcessor = container.GetInstance<ICommandProcessor>();
            this.output = output;
        }

        [Fact]
        public void ApprovalContext_Request()
        {
            var streamId = Guid.NewGuid();

            commandContext.ApprovalContext = new ApprovalContext(ApprovalContext.Action.Request);
            var command = new Employee.Create {StreamId = streamId, Person = new PersonalDetails("John", "Smith")};
            var expected = commandProcessor.Process(command);

            Assert.Equal(1, expected.UpdatedAggregates.Count);
            Assert.Equal(command, expected.FilterEvents<ApprovalRequest.Created>().First().InnerCommand.Command);
        }

        private static readonly Type CommandApproverType = typeof(CommandApprover);

        public object GetCommandUsingReflection(CommandApprover approver, ApprovalRequest.InnerCommand innerCommand)
        {
            var method = CommandApproverType.GetMethod("UnwrapCommand");
            var genericMethod = method.MakeGenericMethod(innerCommand.Type);
            return genericMethod.Invoke(approver, new object[] { innerCommand });
        }

        private void MeasurePerformance(CommandApprover approver, ApprovalRequest.InnerCommand innerCommand, Func<CommandApprover, ApprovalRequest.InnerCommand, object> func)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var cmd = func(approver, innerCommand);
            stopwatch.Stop();
            output.WriteLine("1st call : {0}ms", stopwatch.ElapsedMilliseconds);

            var loop = 20;
            output.WriteLine("Running ({0}) calls....", loop);
            for (var i = 0; i < loop; i++)
            {
                stopwatch.Restart();
                var x = func(approver, innerCommand);
                stopwatch.Stop();
                output.WriteLine("Call ({1}) : {0}us", stopwatch.ElapsedMicroSeconds(), i + 1);
            }

            stopwatch.Restart();
            loop = 1000000;
            for (var i = 0; i < loop; i++)
            {
                var x = func(approver, innerCommand);
            }
            stopwatch.Stop();
            output.WriteLine("Many ({1}) calls : {0}us/call", (decimal) stopwatch.ElapsedMicroSeconds() / loop, loop);
        }

        [RunnableInDebugOnly]
        public void Approval_GetCommand_Performance_UsingReflection()
        {
            // Arrange
            var repository = container.GetInstance<IRepository>();
            var approver = new CommandApprover(repository);
            var id = Guid.NewGuid();
            commandContext.ApprovalContext = new ApprovalContext(ApprovalContext.Action.Request);
            var command = new Employee.Create { StreamId = id, Person = new PersonalDetails("John", "Smith") };
            var result = commandProcessor.Process(command);
            var approvalRequest = repository.Get<ApprovalRequest.Aggregate>(result.UpdatedAggregates.First().StreamId);
            var innerCommand = approvalRequest.GetInnerCommand();

            // Act
            MeasurePerformance(approver, innerCommand, GetCommandUsingReflection);
        }

        [RunnableInDebugOnly]
        public void Approval_GetCommand_Performance_UsingDynamic()
        {
            // Arrange
            var repository = container.GetInstance<IRepository>();
            var approver = new CommandApprover(repository);
            var id = Guid.NewGuid();
            commandContext.ApprovalContext = new ApprovalContext(ApprovalContext.Action.Request);
            var command = new Employee.Create { StreamId = id, Person = new PersonalDetails("John", "Smith") };
            var result = commandProcessor.Process(command);
            var approvalRequest = repository.Get<ApprovalRequest.Aggregate>(result.UpdatedAggregates.First().StreamId);
            var innerCommand = approvalRequest.GetInnerCommand();

            // Act
            MeasurePerformance(approver, innerCommand, (a, c) => a.GetCommand(c));
        }

        [Fact]
        public void ApprovalContext_Approve()
        {
            // Arrange
            var id = Guid.NewGuid();
            commandContext.ApprovalContext = new ApprovalContext(ApprovalContext.Action.Request);
            var command = new Employee.Create { StreamId = id, Person = new PersonalDetails("John", "Smith") };
            var result = commandProcessor.Process(command);
            var approvalRequest = result.FilterEvents<ApprovalRequest.Created>().First();

            // Act
            commandContext.ApprovalContext = new ApprovalContext(ApprovalContext.Action.Approve);
            var expected = commandProcessor.Process(new ApprovalRequest.Approve(approvalRequest.StreamId));
            
            Assert.Equal(approvalRequest.StreamId, expected.FilterEvents<ApprovalRequest.Approved>().First().StreamId);
            Assert.Equal(command.Person.Name, expected.FilterEvents<Person.Created>().First().Person.Name);
        }

        [Fact]
        public void ApprovalContext_Decline()
        {
            // Arrange
            var id = Guid.NewGuid();
            commandContext.ApprovalContext = new ApprovalContext(ApprovalContext.Action.Request);
            var command = new Employee.Create { StreamId = id, Person = new PersonalDetails("John", "Smith") };
            var result = commandProcessor.Process(command);
            var approvalRequest = result.FilterEvents<ApprovalRequest.Created>().First();

            // Act
            commandContext.ApprovalContext = new ApprovalContext(ApprovalContext.Action.Decline);
            var expected = commandProcessor.Process(new ApprovalRequest.Declined {StreamId = approvalRequest.StreamId, Reason = "Sorry"});

            Assert.Equal(approvalRequest.StreamId, expected.FilterEvents<ApprovalRequest.Declined>().First().StreamId);
        }

        [Fact]
        public void ApprovalContext_ApproveMany()
        {
            // Arrange
            var streamId = Guid.NewGuid();

            commandContext.ApprovalContext = new ApprovalContext(ApprovalContext.Action.Request);
            var command = new Person.SendInvite(streamId, new PersonalDetails("John", "Smith"), "john@gmail.com");
            var result = commandProcessor.Process(command);
            var approvalRequest = result.FilterEvents<ApprovalRequest.Created>().First();

            // Act
            commandContext.ApprovalContext = new ApprovalContext(ApprovalContext.Action.Approve);
            var expected = commandProcessor.Process(new ApprovalRequest.Approve(approvalRequest.StreamId));

            var projectedItem = expected.FindProjectedItem<PersonalDetails>();
            Assert.True(projectedItem.FirstName == command.Person.FirstName);

            var person = expected.FilterEvents<Person.Created>().First();
            Assert.True(person.Person.LastName == command.Person.LastName);
            var email = expected.FilterEvents<Email.PersonInvited>().First();
            Assert.True(email.StreamId != streamId);
            Assert.True(email.EmailAddress == command.Email);
            Assert.Equal(4, expected.UpdatedAggregates.Count);
            Assert.Equal(approvalRequest.StreamId, expected.FilterEvents<ApprovalRequest.Approved>().First().StreamId);
            Assert.Equal(command.Person.Name, expected.FilterEvents<Person.Created>().First().Person.Name);
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