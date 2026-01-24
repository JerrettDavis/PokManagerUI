namespace PokManager.Application.Models;

/// <summary>
/// Represents the result of applying configuration changes to a server instance.
/// </summary>
/// <param name="Success">Whether the configuration was applied successfully.</param>
/// <param name="ChangedSettings">List of settings that were changed.</param>
/// <param name="RequiredRestart">Whether the changes require a server restart.</param>
/// <param name="WasRestarted">Whether the server was restarted.</param>
/// <param name="BackupCreated">Whether a backup was created before applying changes.</param>
/// <param name="AppliedAt">When the configuration was applied.</param>
/// <param name="Message">Optional message about the result.</param>
public record ApplyConfigurationResult(
    bool Success,
    IReadOnlyList<string> ChangedSettings,
    bool RequiredRestart,
    bool WasRestarted,
    bool BackupCreated,
    DateTimeOffset AppliedAt,
    string? Message
);
