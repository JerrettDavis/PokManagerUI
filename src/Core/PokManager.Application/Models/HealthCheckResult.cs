using PokManager.Domain.Enumerations;

namespace PokManager.Application.Models;

/// <summary>
/// Represents the result of a health check on a Palworld server instance.
/// </summary>
/// <param name="InstanceId">The instance that was checked.</param>
/// <param name="IsHealthy">Whether the instance is healthy.</param>
/// <param name="Health">The detailed health status.</param>
/// <param name="ResponseTime">How long the health check took.</param>
/// <param name="CheckedAt">When the health check was performed.</param>
/// <param name="Message">Optional message about the health status.</param>
/// <param name="Details">Additional health check details.</param>
public record HealthCheckResult(
    string InstanceId,
    bool IsHealthy,
    ProcessHealth Health,
    TimeSpan ResponseTime,
    DateTimeOffset CheckedAt,
    string? Message,
    IReadOnlyDictionary<string, string>? Details
);
