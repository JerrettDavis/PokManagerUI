namespace PokManager.Application.Models;

/// <summary>
/// Options for applying updates to a Palworld server instance.
/// </summary>
/// <param name="BackupBeforeUpdate">Whether to create a backup before applying updates.</param>
/// <param name="StopInstance">Whether to stop the instance before updating.</param>
/// <param name="StartAfterUpdate">Whether to start the instance after update completes.</param>
/// <param name="ValidateAfterUpdate">Whether to validate the server after updating.</param>
/// <param name="SkipIfNoUpdates">Whether to skip the operation if no updates are available.</param>
public record ApplyUpdatesOptions(
    bool BackupBeforeUpdate = true,
    bool StopInstance = true,
    bool StartAfterUpdate = true,
    bool ValidateAfterUpdate = true,
    bool SkipIfNoUpdates = true
);
