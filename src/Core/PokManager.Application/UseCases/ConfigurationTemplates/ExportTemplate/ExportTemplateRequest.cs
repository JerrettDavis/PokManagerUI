namespace PokManager.Application.UseCases.ConfigurationTemplates.ExportTemplate;

/// <summary>
/// Request to export a configuration template as JSON.
/// </summary>
public record ExportTemplateRequest(
    string TemplateId,
    string CorrelationId = ""
);
