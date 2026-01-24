namespace PokManager.Application.Models;

/// <summary>
/// Request object for creating a new Palworld server instance.
/// </summary>
/// <param name="InstanceId">The unique identifier for the instance.</param>
/// <param name="ServerName">The display name of the server.</param>
/// <param name="Port">The port number the server will listen on.</param>
/// <param name="MaxPlayers">The maximum number of players allowed on the server.</param>
/// <param name="ServerPassword">Optional password required to join the server.</param>
/// <param name="AdminPassword">Password for administrative access to the server.</param>
/// <param name="AutoStart">Whether the instance should start automatically after creation.</param>
/// <param name="AdditionalSettings">Additional key-value configuration settings.</param>
public record CreateInstanceRequest(
    string InstanceId,
    string ServerName,
    int Port,
    int MaxPlayers,
    string? ServerPassword,
    string AdminPassword,
    bool AutoStart = false,
    IReadOnlyDictionary<string, string>? AdditionalSettings = null
);
