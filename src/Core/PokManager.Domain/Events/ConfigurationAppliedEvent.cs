namespace PokManager.Domain.Events;

public class ConfigurationAppliedEvent(string instanceId, string configurationKey, string configurationValue) : DomainEvent
{
    public string InstanceId { get; } = instanceId;
    public string ConfigurationKey { get; } = configurationKey;
    public string ConfigurationValue { get; } = configurationValue;
}
