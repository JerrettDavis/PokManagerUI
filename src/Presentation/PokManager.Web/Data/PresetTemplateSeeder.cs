using Microsoft.EntityFrameworkCore;
using PokManager.Web.Data.Entities;
using System.Text.Json;

namespace PokManager.Web.Data;

/// <summary>
/// Seeds preset configuration templates into the database.
/// </summary>
public static class PresetTemplateSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Seeds preset templates if they don't already exist.
    /// </summary>
    public static async Task SeedPresetsAsync(PokManagerDbContext context)
    {
        // Check if presets already exist
        var existingPresets = await context.ConfigurationTemplates
            .Where(t => t.Type == 0) // Preset type
            .AnyAsync();

        if (existingPresets)
            return; // Already seeded

        var presets = new[]
        {
            CreatePvPCompetitiveTemplate(),
            CreatePvECasualTemplate(),
            CreateBeginnerFriendlyTemplate(),
            CreateHardcoreSurvivalTemplate(),
            CreateBalancedVanillaTemplate()
        };

        await context.ConfigurationTemplates.AddRangeAsync(presets);
        await context.SaveChangesAsync();
    }

    private static ConfigurationTemplate CreatePvPCompetitiveTemplate()
    {
        var config = new Dictionary<string, string>
        {
            ["IsPvE"] = "False",
            ["EnablePlayerToPlayerDamage"] = "True",
            ["EnableFriendlyFire"] = "True",
            ["PlayerDamageRateAttack"] = "1.2",
            ["PlayerDamageRateDefense"] = "0.9",
            ["PalDamageRateAttack"] = "1.0",
            ["PalDamageRateDefense"] = "1.0",
            ["ExpRate"] = "1.5",
            ["PalCaptureRate"] = "1.0",
            ["DayTimeSpeedRate"] = "1.0",
            ["NightTimeSpeedRate"] = "1.0",
            ["EnableFastTravel"] = "False",
            ["DropItemMaxNum"] = "5000",
            ["BaseCampMaxNum"] = "128",
            ["DifficultyLevel"] = "7"
        };

        return new ConfigurationTemplate
        {
            TemplateId = Guid.NewGuid().ToString(),
            Name = "PvP Competitive",
            Description = "Optimized for competitive player-versus-player gameplay with increased player damage and disabled fast travel for strategic combat.",
            Type = 0, // Preset
            IsPartial = false,
            Category = "PvP",
            Difficulty = "Normal",
            MapCompatibility = "",
            Tags = "pvp,competitive,fastpaced",
            ConfigurationDataJson = JsonSerializer.Serialize(config, JsonOptions),
            IncludedSettingsJson = "[]",
            Author = "System",
            TimesUsed = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ConfigurationTemplate CreatePvECasualTemplate()
    {
        var config = new Dictionary<string, string>
        {
            ["IsPvE"] = "True",
            ["EnablePlayerToPlayerDamage"] = "False",
            ["EnableFriendlyFire"] = "False",
            ["PlayerDamageRateAttack"] = "1.5",
            ["PlayerDamageRateDefense"] = "1.2",
            ["PalDamageRateAttack"] = "1.3",
            ["PalDamageRateDefense"] = "1.0",
            ["ExpRate"] = "2.0",
            ["PalCaptureRate"] = "1.5",
            ["PlayerStomachDecreaseRate"] = "0.7",
            ["PlayerStaminaDecreaseRate"] = "0.7",
            ["PlayerAutoHPRegeneRate"] = "1.5",
            ["EnableFastTravel"] = "True",
            ["DifficultyLevel"] = "3",
            ["BaseCampWorkerMaxNum"] = "20"
        };

        return new ConfigurationTemplate
        {
            TemplateId = Guid.NewGuid().ToString(),
            Name = "PvE Casual",
            Description = "Relaxed PvE experience with 2x XP, 1.5x capture rates, and reduced survival needs. Perfect for casual play with friends.",
            Type = 0, // Preset
            IsPartial = false,
            Category = "PvE",
            Difficulty = "Easy",
            MapCompatibility = "",
            Tags = "pve,casual,relaxed,coop",
            ConfigurationDataJson = JsonSerializer.Serialize(config, JsonOptions),
            IncludedSettingsJson = "[]",
            Author = "System",
            TimesUsed = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ConfigurationTemplate CreateBeginnerFriendlyTemplate()
    {
        var config = new Dictionary<string, string>
        {
            ["IsPvE"] = "True",
            ["DifficultyLevel"] = "1",
            ["ExpRate"] = "3.0",
            ["PalCaptureRate"] = "2.0",
            ["PlayerDamageRateAttack"] = "2.0",
            ["PlayerDamageRateDefense"] = "1.5",
            ["PalDamageRateAttack"] = "1.5",
            ["PalDamageRateDefense"] = "0.8",
            ["PlayerStomachDecreaseRate"] = "0.5",
            ["PlayerStaminaDecreaseRate"] = "0.5",
            ["PlayerAutoHPRegeneRate"] = "2.0",
            ["PlayerAutoHpRegeneRateInSleep"] = "3.0",
            ["EnableFastTravel"] = "True",
            ["BaseCampWorkerMaxNum"] = "20",
            ["EnableNonLoginPenalty"] = "False"
        };

        return new ConfigurationTemplate
        {
            TemplateId = Guid.NewGuid().ToString(),
            Name = "Beginner Friendly",
            Description = "Ideal for new players with 3x XP, 2x capture rates, high player damage, and minimal survival challenges. Learn the game without frustration.",
            Type = 0, // Preset
            IsPartial = false,
            Category = "PvE",
            Difficulty = "Beginner",
            MapCompatibility = "",
            Tags = "pve,beginner,easy,learning",
            ConfigurationDataJson = JsonSerializer.Serialize(config, JsonOptions),
            IncludedSettingsJson = "[]",
            Author = "System",
            TimesUsed = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ConfigurationTemplate CreateHardcoreSurvivalTemplate()
    {
        var config = new Dictionary<string, string>
        {
            ["IsPvE"] = "True",
            ["DifficultyLevel"] = "10",
            ["ExpRate"] = "0.5",
            ["PalCaptureRate"] = "0.5",
            ["PlayerDamageRateAttack"] = "0.8",
            ["PlayerDamageRateDefense"] = "0.7",
            ["PalDamageRateAttack"] = "1.5",
            ["PalDamageRateDefense"] = "1.3",
            ["PlayerStomachDecreaseRate"] = "1.5",
            ["PlayerStaminaDecreaseRate"] = "1.5",
            ["PlayerAutoHPRegeneRate"] = "0.5",
            ["EnableFastTravel"] = "False",
            ["EnableNonLoginPenalty"] = "True",
            ["DropItemMaxNum"] = "1000",
            ["BaseCampWorkerMaxNum"] = "10"
        };

        return new ConfigurationTemplate
        {
            TemplateId = Guid.NewGuid().ToString(),
            Name = "Hardcore Survival",
            Description = "Brutal survival challenge with 0.5x XP/capture, high enemy damage, increased hunger/stamina drain, and no fast travel. Only for the bravest.",
            Type = 0, // Preset
            IsPartial = false,
            Category = "PvE",
            Difficulty = "Hardcore",
            MapCompatibility = "",
            Tags = "pve,hardcore,survival,challenge",
            ConfigurationDataJson = JsonSerializer.Serialize(config, JsonOptions),
            IncludedSettingsJson = "[]",
            Author = "System",
            TimesUsed = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ConfigurationTemplate CreateBalancedVanillaTemplate()
    {
        var config = new Dictionary<string, string>
        {
            ["IsPvE"] = "True",
            ["EnablePlayerToPlayerDamage"] = "False",
            ["DifficultyLevel"] = "5",
            ["ExpRate"] = "1.0",
            ["PalCaptureRate"] = "1.0",
            ["PalSpawnNumRate"] = "1.0",
            ["PalDamageRateAttack"] = "1.0",
            ["PalDamageRateDefense"] = "1.0",
            ["PlayerDamageRateAttack"] = "1.0",
            ["PlayerDamageRateDefense"] = "1.0",
            ["PlayerStomachDecreaseRate"] = "1.0",
            ["PlayerStaminaDecreaseRate"] = "1.0",
            ["PlayerAutoHPRegeneRate"] = "1.0",
            ["PlayerAutoHpRegeneRateInSleep"] = "1.0",
            ["DayTimeSpeedRate"] = "1.0",
            ["NightTimeSpeedRate"] = "1.0",
            ["EnableFastTravel"] = "True",
            ["DropItemMaxNum"] = "3000",
            ["BaseCampMaxNum"] = "128",
            ["BaseCampWorkerMaxNum"] = "15"
        };

        return new ConfigurationTemplate
        {
            TemplateId = Guid.NewGuid().ToString(),
            Name = "Balanced Vanilla",
            Description = "Default Palworld experience with all vanilla settings (1.0x rates). Balanced difficulty for standard gameplay as intended by developers.",
            Type = 0, // Preset
            IsPartial = false,
            Category = "Balanced",
            Difficulty = "Normal",
            MapCompatibility = "",
            Tags = "balanced,vanilla,default,standard",
            ConfigurationDataJson = JsonSerializer.Serialize(config, JsonOptions),
            IncludedSettingsJson = "[]",
            Author = "System",
            TimesUsed = 0,
            CreatedAt = DateTime.UtcNow
        };
    }
}
