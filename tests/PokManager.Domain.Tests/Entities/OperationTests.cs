using FluentAssertions;
using PokManager.Domain.Entities;
using PokManager.Domain.Enumerations;
using Xunit;

namespace PokManager.Domain.Tests.Entities;

public class OperationTests
{
    [Fact]
    public void Operation_Can_Be_Created()
    {
        var operation = new Operation(
            OperationType.StartInstance,
            "instance_123");

        operation.OperationType.Should().Be(OperationType.StartInstance);
        operation.InstanceId.Should().Be("instance_123");
        operation.Outcome.Should().Be(OperationOutcome.Pending);
        operation.RequestedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        operation.OperationId.Should().NotBeNullOrEmpty();
        operation.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Operation_Can_Be_Created_Without_InstanceId()
    {
        var operation = new Operation(
            OperationType.CreateBackup,
            null);

        operation.OperationType.Should().Be(OperationType.CreateBackup);
        operation.InstanceId.Should().BeNull();
    }

    [Fact]
    public void Operation_Can_Transition_To_InProgress()
    {
        var operation = new Operation(OperationType.StartInstance, "instance_123");

        var result = operation.Start();

        result.IsSuccess.Should().BeTrue();
        operation.Outcome.Should().Be(OperationOutcome.InProgress);
        operation.StartedAt.Should().NotBeNull();
        operation.StartedAt.Should().BeOnOrAfter(operation.RequestedAt);
    }

    [Fact]
    public void Operation_Can_Transition_To_Completed()
    {
        var operation = new Operation(OperationType.StartInstance, "instance_123");
        operation.Start();

        var result = operation.Complete();

        result.IsSuccess.Should().BeTrue();
        operation.Outcome.Should().Be(OperationOutcome.Completed);
        operation.CompletedAt.Should().NotBeNull();
        operation.CompletedAt.Should().BeOnOrAfter(operation.StartedAt!.Value);
    }

    [Fact]
    public void Operation_Can_Transition_To_Failed()
    {
        var operation = new Operation(OperationType.StartInstance, "instance_123");
        operation.Start();

        var result = operation.Fail("Something went wrong");

        result.IsSuccess.Should().BeTrue();
        operation.Outcome.Should().Be(OperationOutcome.Failed);
        operation.ErrorMessage.Should().Be("Something went wrong");
        operation.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Operation_Cannot_Transition_From_Completed()
    {
        var operation = new Operation(OperationType.StartInstance, "instance_123");
        operation.Start();
        operation.Complete();

        var result = operation.Fail("Error");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot transition from terminal state");
        operation.Outcome.Should().Be(OperationOutcome.Completed);
    }

    [Fact]
    public void Operation_Cannot_Transition_From_Failed()
    {
        var operation = new Operation(OperationType.StartInstance, "instance_123");
        operation.Start();
        operation.Fail("Error");

        var result = operation.Complete();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot transition from terminal state");
        operation.Outcome.Should().Be(OperationOutcome.Failed);
    }

    [Fact]
    public void Operation_Cannot_Complete_Without_Starting()
    {
        var operation = new Operation(OperationType.StartInstance, "instance_123");

        var result = operation.Complete();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot complete operation that hasn't started");
        operation.Outcome.Should().Be(OperationOutcome.Pending);
    }

    [Fact]
    public void Operation_Cannot_Fail_Without_Starting()
    {
        var operation = new Operation(OperationType.StartInstance, "instance_123");

        var result = operation.Fail("Error");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot fail operation that hasn't started");
        operation.Outcome.Should().Be(OperationOutcome.Pending);
    }

    [Fact]
    public void Operation_Timestamps_Are_Consistent()
    {
        var operation = new Operation(OperationType.StartInstance, "instance_123");
        var requestedAt = operation.RequestedAt;

        Thread.Sleep(10); // Small delay to ensure time passes
        operation.Start();
        var startedAt = operation.StartedAt!.Value;

        Thread.Sleep(10);
        operation.Complete();
        var completedAt = operation.CompletedAt!.Value;

        startedAt.Should().BeOnOrAfter(requestedAt);
        completedAt.Should().BeOnOrAfter(startedAt);
    }
}
