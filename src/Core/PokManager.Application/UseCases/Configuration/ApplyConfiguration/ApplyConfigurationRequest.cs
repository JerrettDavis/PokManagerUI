namespace PokManager.Application.UseCases.Configuration.ApplyConfiguration;

public record ApplyConfigurationRequest(
    string InstanceId,
    string CorrelationId,
    Dictionary<string, string> ConfigurationSettings,
    bool RestartInstance = false);
