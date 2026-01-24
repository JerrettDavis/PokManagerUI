namespace PokManager.Web.Data.Entities;

/// <summary>
/// Represents a reusable configuration template for Palworld server instances.
/// Templates can be system-provided presets or user-created, and can include all settings or a subset (partial template).
/// </summary>
public class ConfigurationTemplate
{
    /// <summary>
    /// Primary key for database storage.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier for the template (GUID). Used for external references and API operations.
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the template (e.g., "PvP Competitive", "Beginner Friendly").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description explaining what this template does and when to use it.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Template type: 0 = Preset (system-provided), 1 = UserCreated.
    /// Preset templates cannot be edited or deleted.
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// Indicates whether this is a partial template (applies only selected settings)
    /// or a full template (applies all configuration settings).
    /// </summary>
    public bool IsPartial { get; set; }

    /// <summary>
    /// Timestamp when the template was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the template was last updated (null if never updated).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Organization & Classification

    /// <summary>
    /// Category for organizing templates (e.g., "PvP", "PvE", "Balanced", "Custom").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Difficulty level indicator (e.g., "Beginner", "Normal", "Hardcore").
    /// </summary>
    public string Difficulty { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of compatible maps (e.g., "TheIsland,Ragnarok").
    /// Empty string means compatible with all maps.
    /// </summary>
    public string MapCompatibility { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of tags for filtering and search (e.g., "fastpaced,highexp,pvp").
    /// </summary>
    public string Tags { get; set; } = string.Empty;

    // Template Data

    /// <summary>
    /// JSON-serialized ServerConfiguration object containing the template's configuration settings.
    /// For partial templates, only includes the settings specified in IncludedSettingsJson.
    /// </summary>
    public string ConfigurationDataJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized array of property names included in this template.
    /// For full templates, this is empty or contains all properties.
    /// For partial templates, contains only the selected property names (e.g., ["ExpRate", "PalCaptureRate"]).
    /// </summary>
    public string IncludedSettingsJson { get; set; } = string.Empty;

    // Metadata

    /// <summary>
    /// Author of the template. "System" for presets, username for user-created templates.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Counter tracking how many times this template has been applied to instances.
    /// Used for popularity metrics.
    /// </summary>
    public int TimesUsed { get; set; } = 0;
}
