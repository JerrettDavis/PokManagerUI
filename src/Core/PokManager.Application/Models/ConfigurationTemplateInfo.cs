namespace PokManager.Application.Models;

/// <summary>
/// Represents configuration template metadata and data.
/// Used for transferring template information between application layers.
/// </summary>
public record ConfigurationTemplateInfo(
    string TemplateId,
    string Name,
    string Description,
    int Type, // 0=Preset, 1=UserCreated
    bool IsPartial,
    string Category,
    string Difficulty,
    string[] MapCompatibility,
    string[] Tags,
    IReadOnlyDictionary<string, string> ConfigurationData,
    string[] IncludedSettings, // Property names included in partial templates
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string Author,
    int TimesUsed
);

/// <summary>
/// Represents a single setting change when comparing configurations.
/// </summary>
public record SettingChange(
    string SettingName,        // Property name (e.g., "ExpRate")
    string DisplayName,         // Human-readable name (e.g., "Experience Rate")
    string CurrentValue,        // Current value as string
    string NewValue,            // New value from template as string
    string Category             // Setting category (e.g., "Gameplay", "Combat")
);

/// <summary>
/// Represents a preview of changes that would be applied by a template.
/// </summary>
public record TemplatePreview(
    IReadOnlyList<SettingChange> Changes,
    bool IsCompatible,           // Whether template is compatible with target instance
    string[] Warnings            // Compatibility warnings or important notes
);
