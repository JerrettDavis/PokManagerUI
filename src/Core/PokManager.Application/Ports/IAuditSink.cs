using PokManager.Application.Models;
using PokManager.Domain.Common;

namespace PokManager.Application.Ports;

/// <summary>
/// Interface for persisting and querying audit events.
/// Provides a write-through sink for audit logs and query capabilities for audit trail analysis.
/// </summary>
public interface IAuditSink
{
    /// <summary>
    /// Emits an audit event to the sink for persistence.
    /// </summary>
    /// <param name="auditEvent">The audit event to emit.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EmitAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries audit events based on the specified criteria.
    /// </summary>
    /// <param name="query">The query parameters to filter audit events.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing a read-only list of matching audit events.</returns>
    Task<Result<IReadOnlyList<AuditEvent>>> QueryAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents query parameters for filtering audit events.
/// </summary>
/// <param name="InstanceId">Filter by specific instance identifier (null for all instances).</param>
/// <param name="OperationType">Filter by operation type (null for all types).</param>
/// <param name="StartTime">Filter events occurring on or after this time (null for no lower bound).</param>
/// <param name="EndTime">Filter events occurring on or before this time (null for no upper bound).</param>
/// <param name="MaxResults">Maximum number of results to return (default: 100).</param>
public record AuditQuery(
    string? InstanceId = null,
    string? OperationType = null,
    DateTimeOffset? StartTime = null,
    DateTimeOffset? EndTime = null,
    int MaxResults = 100
);
