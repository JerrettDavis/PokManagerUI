namespace PokManager.Domain.Events;

public class BackupRestoredEvent(string backupId, string instanceId, DateTimeOffset restoredAt) : DomainEvent
{
    public string BackupId { get; } = backupId;
    public string InstanceId { get; } = instanceId;
    public DateTimeOffset RestoredAt { get; } = restoredAt;
}
