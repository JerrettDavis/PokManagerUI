namespace PokManager.Application.Models;

/// <summary>
/// Represents information about available updates for a Palworld server instance.
/// </summary>
/// <param name="IsUpdateAvailable">Whether updates are available.</param>
/// <param name="CurrentVersion">The currently installed version.</param>
/// <param name="LatestVersion">The latest available version.</param>
/// <param name="ReleaseNotes">Release notes for the latest version.</param>
/// <param name="EstimatedDownloadSizeBytes">Estimated size of the update download in bytes.</param>
/// <param name="RequiresRestart">Whether applying the update requires a server restart.</param>
/// <param name="CheckedAt">When this update check was performed.</param>
public record UpdateAvailability(
    bool IsUpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? ReleaseNotes,
    long? EstimatedDownloadSizeBytes,
    bool RequiresRestart,
    DateTimeOffset CheckedAt
);
