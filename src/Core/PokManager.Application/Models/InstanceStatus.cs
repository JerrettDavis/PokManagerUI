using PokManager.Domain.Enumerations;

namespace PokManager.Application.Models;

/// <summary>
/// Represents the current status of a Palworld server instance.
/// </summary>
/// <param name="InstanceId">The unique identifier of the instance.</param>
/// <param name="State">The current state of the instance.</param>
/// <param name="Health">The health status of the instance process.</param>
/// <param name="Uptime">How long the instance has been running.</param>
/// <param name="PlayerCount">Current number of connected players.</param>
/// <param name="MaxPlayers">Maximum number of players allowed.</param>
/// <param name="Version">The current server version.</param>
/// <param name="LastUpdated">When this status information was last updated.</param>
public record InstanceStatus(
    string InstanceId,
    InstanceState State,
    ProcessHealth Health,
    TimeSpan? Uptime,
    int PlayerCount,
    int MaxPlayers,
    string? Version,
    DateTimeOffset LastUpdated
);
