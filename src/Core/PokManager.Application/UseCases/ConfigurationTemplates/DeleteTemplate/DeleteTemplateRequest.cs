namespace PokManager.Application.UseCases.ConfigurationTemplates.DeleteTemplate;

/// <summary>
/// Request to delete a user-created configuration template.
/// </summary>
public record DeleteTemplateRequest(
    string TemplateId,
    string CorrelationId = ""
);
