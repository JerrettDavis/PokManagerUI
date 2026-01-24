namespace PokManager.Application.UseCases.ConfigurationTemplates.SaveTemplate;

/// <summary>
/// Response after successfully saving a template.
/// </summary>
public record SaveTemplateResponse(
    string TemplateId,
    string Name,
    DateTimeOffset CreatedAt
);
