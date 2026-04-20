using System.Collections.Concurrent;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Infrastructure.Fakes;

public class InMemoryAuditSink : IAuditSink
{
    private readonly ConcurrentBag<AuditEvent> _events = new();

    public Task EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        _events.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task<Result<IReadOnlyList<AuditEvent>>> QueryAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        var events = _events.AsEnumerable();

        if (!string.IsNullOrEmpty(query.InstanceId))
            events = events.Where(e => e.InstanceId == query.InstanceId);

        if (!string.IsNullOrEmpty(query.OperationType))
            events = events.Where(e => e.OperationType == query.OperationType);

        if (query.StartTime.HasValue)
            events = events.Where(e => e.PerformedAt >= query.StartTime.Value);

        if (query.EndTime.HasValue)
            events = events.Where(e => e.PerformedAt <= query.EndTime.Value);

        var result = events
            .OrderByDescending(e => e.PerformedAt)
            .Take(query.MaxResults)
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<AuditEvent>>.Success(result.AsReadOnly()));
    }

    public IReadOnlyList<AuditEvent> GetAllEvents() => _events.ToList();

    public void Reset()
    {
        _events.Clear();
    }
}
