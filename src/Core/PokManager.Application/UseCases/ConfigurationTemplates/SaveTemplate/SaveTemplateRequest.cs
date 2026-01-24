namespace PokManager.Application.UseCases.ConfigurationTemplates.SaveTemplate;

/// <summary>
/// Request to save a new configuration template.
/// </summary>
public record SaveTemplateRequest(
    string Name,
    string Description,
    string Category,
    string Difficulty,
    IReadOnlyDictionary<string, string> ConfigurationSettings,
    string[]? IncludedSettings = null, // For partial templates; null = full template
    string[]? MapCompatibility = null,
    string[]? Tags = null,
    string Author = "User",
    string CorrelationId = ""
);
