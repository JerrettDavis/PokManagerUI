namespace PokManager.Application.UseCases.ConfigurationTemplates.ApplyTemplate;

/// <summary>
/// Request to apply a configuration template to an instance.
/// </summary>
public record ApplyTemplateRequest(
    string TemplateId,
    string InstanceId,
    bool CreateBackup = true,
    bool RestartIfNeeded = true,
    string CorrelationId = ""
);
