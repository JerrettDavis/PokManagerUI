using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;

/// <summary>
/// Handler for retrieving the configuration of a Palworld server instance.
/// </summary>
public class GetConfigurationHandler(IPokManagerClient client)
{
    private const string MaskedPassword = "********";
    private readonly IPokManagerClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly GetConfigurationRequestValidator _validator = new();

    /// <summary>
    /// Handles the GetConfiguration request.
    /// </summary>
    /// <param name="request">The request containing the instance ID and options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the configuration response or an error.</returns>
    public async Task<Result<GetConfigurationResponse>> Handle(
        GetConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        // Validate the request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<GetConfigurationResponse>(errors);
        }

        // Get instance details from the client
        var detailsResult = await _client.GetInstanceDetailsAsync(request.InstanceId, cancellationToken);
        if (detailsResult.IsFailure)
        {
            return Result.Failure<GetConfigurationResponse>(detailsResult.Error);
        }

        var instanceDetails = detailsResult.Value;
        var rawConfiguration = instanceDetails.Configuration;

        // Extract standard configuration fields
        var sessionName = rawConfiguration.TryGetValue("SessionName", out var name) ? name : string.Empty;
        var serverPassword = rawConfiguration.TryGetValue("ServerPassword", out var password) ? password : string.Empty;
        var maxPlayersStr = rawConfiguration.TryGetValue("MaxPlayers", out var maxPlayers) ? maxPlayers : "0";
        var serverMap = rawConfiguration.TryGetValue("ServerMap", out var map) ? map : string.Empty;
        var modsStr = rawConfiguration.TryGetValue("Mods", out var mods) ? mods : string.Empty;

        // Parse MaxPlayers
        if (!int.TryParse(maxPlayersStr, out var maxPlayersInt))
        {
            maxPlayersInt = 0;
        }

        // Parse Mods list (comma-separated)
        var modsList = string.IsNullOrWhiteSpace(modsStr)
            ? Array.Empty<string>()
            : modsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Extract custom settings (all settings except the standard ones)
        var standardKeys = new HashSet<string> { "SessionName", "ServerPassword", "MaxPlayers", "ServerMap", "Mods" };
        var customSettings = rawConfiguration
            .Where(kvp => !standardKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Mask the password if IncludeSecrets is false
        var finalPassword = request.IncludeSecrets ? serverPassword : MaskedPassword;

        // Build the configuration DTO
        var configDto = new ConfigurationDto(
            sessionName,
            finalPassword,
            maxPlayersInt,
            serverMap,
            modsList,
            customSettings);

        var response = new GetConfigurationResponse(configDto);
        return Result<GetConfigurationResponse>.Success(response);
    }
}
