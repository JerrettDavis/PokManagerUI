namespace PokManager.Application.UseCases.ConfigurationTemplates.ImportTemplate;

/// <summary>
/// Response after successfully importing a template.
/// </summary>
public record ImportTemplateResponse(
    string TemplateId,
    string TemplateName,
    DateTimeOffset ImportedAt
);
