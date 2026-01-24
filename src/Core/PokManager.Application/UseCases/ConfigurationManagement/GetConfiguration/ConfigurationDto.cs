namespace PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;

/// <summary>
/// Data transfer object containing Palworld server configuration settings.
/// </summary>
/// <param name="SessionName">The display name of the server session.</param>
/// <param name="ServerPassword">The server password (masked with "********" if IncludeSecrets is false).</param>
/// <param name="MaxPlayers">The maximum number of players allowed on the server.</param>
/// <param name="ServerMap">The map being used by the server.</param>
/// <param name="Mods">List of mod identifiers installed on the server.</param>
/// <param name="CustomSettings">Dictionary of additional custom settings not covered by standard fields.</param>
public record ConfigurationDto(
    string SessionName,
    string ServerPassword,
    int MaxPlayers,
    string ServerMap,
    IReadOnlyList<string> Mods,
    IReadOnlyDictionary<string, string> CustomSettings);
