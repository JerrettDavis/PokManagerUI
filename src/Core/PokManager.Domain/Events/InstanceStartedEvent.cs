namespace PokManager.Domain.Events;

public class InstanceStartedEvent(string instanceId, DateTimeOffset startedAt) : DomainEvent
{
    public string InstanceId { get; } = instanceId;
    public DateTimeOffset StartedAt { get; } = startedAt;
}
