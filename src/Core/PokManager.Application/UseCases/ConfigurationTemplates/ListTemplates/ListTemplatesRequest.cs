namespace PokManager.Application.UseCases.ConfigurationTemplates.ListTemplates;

/// <summary>
/// Request to list configuration templates with optional filters.
/// </summary>
public record ListTemplatesRequest(
    string? Category = null,
    int? Type = null, // 0=Preset, 1=UserCreated, null=All
    string? MapFilter = null,
    string CorrelationId = ""
);
