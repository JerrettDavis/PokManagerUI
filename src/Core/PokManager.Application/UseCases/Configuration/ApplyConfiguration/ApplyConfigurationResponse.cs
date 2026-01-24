namespace PokManager.Application.UseCases.Configuration.ApplyConfiguration;

/// <summary>
/// Response containing the result of applying configuration changes to a server instance.
/// </summary>
/// <param name="Success">Whether the configuration was applied successfully.</param>
/// <param name="InstanceId">The instance identifier the configuration was applied to.</param>
/// <param name="ChangedSettings">List of configuration settings that were changed.</param>
/// <param name="RequiresRestart">Whether the changes require a server restart to take effect.</param>
/// <param name="WasRestarted">Whether the server was automatically restarted.</param>
/// <param name="BackupCreated">Whether a backup was created before applying changes.</param>
/// <param name="AppliedAt">When the configuration was applied.</param>
/// <param name="Message">Optional message about the result.</param>
public record ApplyConfigurationResponse(
    bool Success,
    string InstanceId,
    IReadOnlyList<string> ChangedSettings,
    bool RequiresRestart,
    bool WasRestarted,
    bool BackupCreated,
    DateTimeOffset AppliedAt,
    string? Message
);
