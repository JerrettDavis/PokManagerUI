using PokManager.Domain.Enumerations;

namespace PokManager.Application.UseCases.InstanceQuery;

/// <summary>
/// Data transfer object containing instance status information.
/// </summary>
/// <param name="Id">The unique identifier of the instance.</param>
/// <param name="State">The current state of the instance.</param>
/// <param name="LastCheckedAt">When this status was last checked.</param>
/// <param name="ContainerId">The container identifier (if applicable).</param>
/// <param name="Health">The health status of the instance.</param>
/// <param name="Uptime">How long the instance has been running (if running).</param>
/// <param name="PlayerCount">Current number of connected players.</param>
/// <param name="MaxPlayers">Maximum number of players allowed.</param>
/// <param name="Version">The current server version.</param>
public record InstanceStatusDto(
    string Id,
    InstanceState State,
    DateTimeOffset LastCheckedAt,
    string? ContainerId,
    ProcessHealth Health,
    TimeSpan? Uptime,
    int PlayerCount,
    int MaxPlayers,
    string? Version);
