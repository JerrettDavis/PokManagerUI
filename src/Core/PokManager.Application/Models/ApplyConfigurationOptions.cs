namespace PokManager.Application.Models;

/// <summary>
/// Options for applying configuration changes to a Palworld server instance.
/// </summary>
/// <param name="ValidateBeforeApply">Whether to validate the configuration before applying it.</param>
/// <param name="BackupBeforeApply">Whether to backup the current configuration before applying changes.</param>
/// <param name="RestartIfNeeded">Whether to restart the instance if configuration changes require it.</param>
/// <param name="DryRun">Whether to perform a dry run without actually applying changes.</param>
public record ApplyConfigurationOptions(
    bool ValidateBeforeApply = true,
    bool BackupBeforeApply = true,
    bool RestartIfNeeded = true,
    bool DryRun = false
);
