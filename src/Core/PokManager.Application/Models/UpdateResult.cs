namespace PokManager.Application.Models;

/// <summary>
/// Represents the result of an update operation on a Palworld server instance.
/// </summary>
/// <param name="Success">Whether the update was successful.</param>
/// <param name="PreviousVersion">The version before the update.</param>
/// <param name="NewVersion">The version after the update.</param>
/// <param name="UpdatedAt">When the update was applied.</param>
/// <param name="Duration">How long the update took to apply.</param>
/// <param name="Message">Optional message about the update result.</param>
/// <param name="RequiredRestart">Whether a restart was required.</param>
/// <param name="WasRestarted">Whether the instance was restarted.</param>
public record UpdateResult(
    bool Success,
    string PreviousVersion,
    string NewVersion,
    DateTimeOffset UpdatedAt,
    TimeSpan Duration,
    string? Message,
    bool RequiredRestart,
    bool WasRestarted
);
