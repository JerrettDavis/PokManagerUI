namespace PokManager.Application.UseCases.ConfigurationTemplates.PreviewTemplate;

/// <summary>
/// Request to preview what changes would be applied by a template to an instance.
/// </summary>
public record PreviewTemplateRequest(
    string TemplateId,
    string InstanceId,
    string CorrelationId = ""
);
