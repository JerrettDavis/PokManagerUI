using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.ConfigurationTemplates.PreviewTemplate;

/// <summary>
/// Handler for previewing template changes before applying them.
/// Compares template settings with current instance configuration.
/// </summary>
public class PreviewTemplateHandler
{
    private readonly IConfigurationTemplateStore _templateStore;
    private readonly GetConfigurationHandler _getConfigHandler;
    private readonly PreviewTemplateValidator _validator;

    public PreviewTemplateHandler(
        IConfigurationTemplateStore templateStore,
        GetConfigurationHandler getConfigHandler)
    {
        _templateStore = templateStore;
        _getConfigHandler = getConfigHandler;
        _validator = new PreviewTemplateValidator();
    }

    public async Task<Result<PreviewTemplateResponse>> Handle(
        PreviewTemplateRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<PreviewTemplateResponse>(
                $"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        // Get template
        var templateResult = await _templateStore.GetTemplateAsync(request.TemplateId, cancellationToken);
        if (templateResult.IsFailure)
            return Result.Failure<PreviewTemplateResponse>(templateResult.Error);

        var template = templateResult.Value;

        // Get current instance configuration
        var currentConfigResult = await _getConfigHandler.Handle(
            new GetConfigurationRequest(request.CorrelationId, request.InstanceId, IncludeSecrets: false),
            cancellationToken);

        if (currentConfigResult.IsFailure)
            return Result.Failure<PreviewTemplateResponse>(currentConfigResult.Error);

        var currentConfig = currentConfigResult.Value.Configuration;
        var currentConfigDict = BuildConfigurationDictionary(currentConfig);

        // Build list of changes
        var changes = new List<SettingChange>();
        var warnings = new List<string>();

        // For partial templates, only include settings specified in IncludedSettings
        var settingsToCompare = template.IsPartial && template.IncludedSettings.Length > 0
            ? template.ConfigurationData.Where(kvp => template.IncludedSettings.Contains(kvp.Key))
            : template.ConfigurationData;

        foreach (var (settingName, newValue) in settingsToCompare)
        {
            var currentValue = currentConfigDict.GetValueOrDefault(settingName, "");

            // Only add to changes if values are different
            if (currentValue != newValue)
            {
                var displayName = FormatDisplayName(settingName);
                var category = DetermineCategory(settingName);

                changes.Add(new SettingChange(
                    settingName,
                    displayName,
                    currentValue,
                    newValue,
                    category
                ));
            }
        }

        // Check compatibility
        bool isCompatible = true;

        // Check map compatibility if specified in template
        if (template.MapCompatibility.Length > 0)
        {
            var currentMap = currentConfigDict.GetValueOrDefault("ServerMap", "");
            if (!string.IsNullOrWhiteSpace(currentMap) &&
                !template.MapCompatibility.Contains(currentMap, StringComparer.OrdinalIgnoreCase))
            {
                isCompatible = false;
                warnings.Add($"Template is designed for maps: {string.Join(", ", template.MapCompatibility)}. " +
                           $"Current instance is using map: {currentMap}. Some settings may not be optimal.");
            }
        }

        // Add informational warnings
        if (template.IsPartial)
        {
            warnings.Add($"This is a partial template affecting {changes.Count} settings. Other settings will remain unchanged.");
        }
        else
        {
            warnings.Add($"This is a full template that will modify {changes.Count} settings.");
        }

        return Result<PreviewTemplateResponse>.Success(new PreviewTemplateResponse(
            template.TemplateId,
            template.Name,
            request.InstanceId,
            changes,
            isCompatible,
            warnings.ToArray()
        ));
    }

    private Dictionary<string, string> BuildConfigurationDictionary(ConfigurationDto config)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SessionName"] = config.SessionName,
            ["ServerPassword"] = config.ServerPassword,
            ["MaxPlayers"] = config.MaxPlayers.ToString(),
            ["ServerMap"] = config.ServerMap
        };

        if (config.Mods.Count > 0)
        {
            dict["Mods"] = string.Join(",", config.Mods);
        }

        foreach (var (key, value) in config.CustomSettings)
        {
            dict[key] = value;
        }

        return dict;
    }

    private string FormatDisplayName(string settingName)
    {
        // Convert camelCase/PascalCase to readable format
        // e.g., "ExpRate" -> "Exp Rate", "PlayerDamageRateAttack" -> "Player Damage Rate Attack"
        return System.Text.RegularExpressions.Regex.Replace(
            settingName,
            "([a-z])([A-Z])",
            "$1 $2"
        );
    }

    private string DetermineCategory(string settingName)
    {
        // Categorize settings based on their names
        if (settingName.Contains("Damage", StringComparison.OrdinalIgnoreCase) ||
            settingName.Contains("Defense", StringComparison.OrdinalIgnoreCase))
            return "Combat";

        if (settingName.Contains("Exp", StringComparison.OrdinalIgnoreCase) ||
            settingName.Contains("Capture", StringComparison.OrdinalIgnoreCase))
            return "Progression";

        if (settingName.Contains("Stamina", StringComparison.OrdinalIgnoreCase) ||
            settingName.Contains("Stomach", StringComparison.OrdinalIgnoreCase) ||
            settingName.Contains("HP", StringComparison.OrdinalIgnoreCase) ||
            settingName.Contains("Regen", StringComparison.OrdinalIgnoreCase))
            return "Survival";

        if (settingName.Contains("Day", StringComparison.OrdinalIgnoreCase) ||
            settingName.Contains("Night", StringComparison.OrdinalIgnoreCase) ||
            settingName.Contains("Speed", StringComparison.OrdinalIgnoreCase))
            return "World";

        if (settingName.Contains("PvP", StringComparison.OrdinalIgnoreCase) ||
            settingName.Contains("PvE", StringComparison.OrdinalIgnoreCase) ||
            settingName.Contains("Player", StringComparison.OrdinalIgnoreCase) ||
            settingName.Contains("Friendly", StringComparison.OrdinalIgnoreCase))
            return "PvP/PvE";

        return "General";
    }
}
