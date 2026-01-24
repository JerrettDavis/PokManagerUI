namespace PokManager.Application.UseCases.ConfigurationTemplates.ApplyTemplate;

/// <summary>
/// Response after applying a configuration template.
/// </summary>
public record ApplyTemplateResponse(
    bool Success,
    string TemplateId,
    string TemplateName,
    string InstanceId,
    string[] ChangedSettings,
    bool BackupCreated,
    string? BackupId,
    bool RequiredRestart,
    bool WasRestarted,
    DateTimeOffset AppliedAt,
    string Message
);
