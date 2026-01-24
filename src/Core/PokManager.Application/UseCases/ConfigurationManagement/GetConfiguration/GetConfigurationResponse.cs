namespace PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;

/// <summary>
/// Response containing the configuration of a Palworld server instance.
/// </summary>
/// <param name="Configuration">The server configuration details.</param>
public record GetConfigurationResponse(
    ConfigurationDto Configuration);
