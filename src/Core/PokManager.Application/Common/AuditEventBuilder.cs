using PokManager.Domain.Enumerations;

namespace PokManager.Application.Common;

/// <summary>
/// Fluent builder for creating audit events.
/// </summary>
public class AuditEventBuilder
{
    private string? _operationType;
    private string? _instanceId;
    private string? _correlationId;
    private OperationOutcome _outcome = OperationOutcome.Pending;
    private string? _errorMessage;
    private Dictionary<string, object> _metadata = new();

    public AuditEventBuilder WithOperationType(string operationType)
    {
        _operationType = operationType;
        return this;
    }

    public AuditEventBuilder WithInstanceId(string instanceId)
    {
        _instanceId = instanceId;
        return this;
    }

    public AuditEventBuilder WithCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    public AuditEventBuilder WithOutcome(OperationOutcome outcome)
    {
        _outcome = outcome;
        return this;
    }

    public AuditEventBuilder WithError(string errorMessage)
    {
        _errorMessage = errorMessage;
        _outcome = OperationOutcome.Failed;
        return this;
    }

    public AuditEventBuilder WithMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    public AuditEvent Build()
    {
        return new AuditEvent
        {
            OperationType = _operationType ?? throw new InvalidOperationException("OperationType is required"),
            InstanceId = _instanceId,
            CorrelationId = _correlationId ?? throw new InvalidOperationException("CorrelationId is required"),
            Outcome = _outcome,
            ErrorMessage = _errorMessage,
            Metadata = _metadata,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

// Simple AuditEvent DTO
public class AuditEvent
{
    public string OperationType { get; init; } = null!;
    public string? InstanceId { get; init; }
    public string CorrelationId { get; init; } = null!;
    public OperationOutcome Outcome { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; }
}
