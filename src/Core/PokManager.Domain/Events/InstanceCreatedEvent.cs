namespace PokManager.Domain.Events;

public class InstanceCreatedEvent(string instanceId, string sessionName, string mapName) : DomainEvent
{
    public string InstanceId { get; } = instanceId;
    public string SessionName { get; } = sessionName;
    public string MapName { get; } = mapName;
}
