namespace PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;

/// <summary>
/// Request to retrieve the configuration of a Palworld server instance.
/// </summary>
/// <param name="InstanceId">The unique identifier of the instance.</param>
/// <param name="CorrelationId">The correlation ID for tracking this request.</param>
/// <param name="IncludeSecrets">Whether to include sensitive data like passwords in the response. Defaults to false.</param>
public record GetConfigurationRequest(
    string InstanceId,
    string CorrelationId,
    bool IncludeSecrets = false);
