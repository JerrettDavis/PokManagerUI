namespace PokManager.Domain.Events;

public class InstanceStoppedEvent(string instanceId, DateTimeOffset stoppedAt) : DomainEvent
{
    public string InstanceId { get; } = instanceId;
    public DateTimeOffset StoppedAt { get; } = stoppedAt;
}
