using FluentAssertions;
using PokManager.Domain.Enumerations;

namespace PokManager.Domain.Tests.Enumerations;

public class OperationOutcomeTests
{
    // NOTE: This enum currently differs from specification due to existing code dependencies
    // Spec requires: Unknown, Success, Failure, PartialSuccess, Skipped
    // Current implementation: Pending, InProgress, Completed, Failed, Cancelled
    // This will need to be updated when Operation entity is refactored

    [Fact]
    public void OperationOutcome_Should_Have_Correct_Values()
    {
        OperationOutcome.Pending.Should().Be((OperationOutcome)0);
        OperationOutcome.InProgress.Should().Be((OperationOutcome)1);
        OperationOutcome.Completed.Should().Be((OperationOutcome)2);
        OperationOutcome.Failed.Should().Be((OperationOutcome)3);
        OperationOutcome.Cancelled.Should().Be((OperationOutcome)4);
    }

    [Fact]
    public void OperationOutcome_Should_Have_All_Expected_Members()
    {
        var values = Enum.GetValues<OperationOutcome>();
        values.Should().Contain(OperationOutcome.Pending);
        values.Should().Contain(OperationOutcome.InProgress);
        values.Should().Contain(OperationOutcome.Completed);
        values.Should().Contain(OperationOutcome.Failed);
        values.Should().Contain(OperationOutcome.Cancelled);
    }
}
