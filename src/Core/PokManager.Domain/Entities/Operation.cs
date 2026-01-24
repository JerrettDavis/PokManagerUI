using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Domain.Entities;

public class Operation(OperationType operationType, string? instanceId)
{
    public string OperationId { get; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; } = Guid.NewGuid().ToString();
    public OperationType OperationType { get; } = operationType;
    public string? InstanceId { get; } = instanceId;
    public DateTimeOffset RequestedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public OperationOutcome Outcome { get; private set; } = OperationOutcome.Pending;
    public string? ErrorMessage { get; private set; }

    public Result<Unit> Start()
    {
        if (IsTerminalState())
        {
            return Result.Failure<Unit>("Cannot transition from terminal state");
        }

        Outcome = OperationOutcome.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result<Unit> Complete()
    {
        if (IsTerminalState())
        {
            return Result.Failure<Unit>("Cannot transition from terminal state");
        }

        if (Outcome == OperationOutcome.Pending)
        {
            return Result.Failure<Unit>("Cannot complete operation that hasn't started");
        }

        Outcome = OperationOutcome.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result<Unit> Fail(string errorMessage)
    {
        if (IsTerminalState())
        {
            return Result.Failure<Unit>("Cannot transition from terminal state");
        }

        if (Outcome == OperationOutcome.Pending)
        {
            return Result.Failure<Unit>("Cannot fail operation that hasn't started");
        }

        Outcome = OperationOutcome.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    private bool IsTerminalState()
    {
        return Outcome == OperationOutcome.Completed ||
               Outcome == OperationOutcome.Failed ||
               Outcome == OperationOutcome.Cancelled;
    }
}
