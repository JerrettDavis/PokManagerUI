namespace PokManager.Application.UseCases.ConfigurationTemplates.ListTemplates;

/// <summary>
/// Response containing a list of template summaries.
/// </summary>
public record ListTemplatesResponse(
    IReadOnlyList<TemplateSummaryDto> Templates
);

/// <summary>
/// Summary information about a configuration template (for list display).
/// </summary>
public record TemplateSummaryDto(
    string TemplateId,
    string Name,
    string Description,
    int Type, // 0=Preset, 1=UserCreated
    string Category,
    string Difficulty,
    bool IsPartial,
    int TimesUsed,
    DateTimeOffset CreatedAt,
    string[] Tags
);
