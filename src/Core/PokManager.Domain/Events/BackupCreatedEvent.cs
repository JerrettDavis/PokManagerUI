namespace PokManager.Domain.Events;

public class BackupCreatedEvent(string backupId, string instanceId, long backupSize) : DomainEvent
{
    public string BackupId { get; } = backupId;
    public string InstanceId { get; } = instanceId;
    public long BackupSize { get; } = backupSize;
}
