namespace PokManager.Application.Models;

/// <summary>
/// Represents an audit event that tracks operations performed on server instances.
/// </summary>
/// <param name="EventId">Unique identifier for this audit event.</param>
/// <param name="InstanceId">The instance the operation was performed on.</param>
/// <param name="OperationType">The type of operation performed.</param>
/// <param name="PerformedBy">The user or system that performed the operation.</param>
/// <param name="PerformedAt">When the operation was performed.</param>
/// <param name="Outcome">The outcome of the operation.</param>
/// <param name="Duration">How long the operation took.</param>
/// <param name="Details">Additional details about the operation.</param>
/// <param name="ErrorMessage">Error message if the operation failed.</param>
public record AuditEvent(
    Guid EventId,
    string InstanceId,
    string OperationType,
    string PerformedBy,
    DateTimeOffset PerformedAt,
    string Outcome,
    TimeSpan? Duration,
    IReadOnlyDictionary<string, string>? Details,
    string? ErrorMessage
);
