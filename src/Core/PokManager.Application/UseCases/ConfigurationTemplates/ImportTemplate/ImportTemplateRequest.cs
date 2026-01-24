namespace PokManager.Application.UseCases.ConfigurationTemplates.ImportTemplate;

/// <summary>
/// Request to import a configuration template from JSON.
/// </summary>
public record ImportTemplateRequest(
    Stream TemplateData,
    string CorrelationId = ""
);
