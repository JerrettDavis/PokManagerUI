using PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;
using PokManager.Application.UseCases.Configuration.ApplyConfiguration;
using PokManager.Domain.Common;
using PokManager.Web.Models;

namespace PokManager.Web.Services;

/// <summary>
/// Facade service for configuration operations, bridging UI layer and Application handlers.
/// </summary>
public class ConfigurationService
{
    private readonly GetConfigurationHandler _getConfigurationHandler;
    private readonly ApplyConfigurationHandler _applyConfigurationHandler;

    public ConfigurationService(
        GetConfigurationHandler getConfigurationHandler,
        ApplyConfigurationHandler applyConfigurationHandler)
    {
        _getConfigurationHandler = getConfigurationHandler ?? throw new ArgumentNullException(nameof(getConfigurationHandler));
        _applyConfigurationHandler = applyConfigurationHandler ?? throw new ArgumentNullException(nameof(applyConfigurationHandler));
    }

    /// <summary>
    /// Retrieves the current configuration for an instance.
    /// </summary>
    public async Task<Result<ConfigurationViewModel>> GetConfigurationAsync(
        string instanceId,
        bool includeSecrets = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _getConfigurationHandler.Handle(
            new GetConfigurationRequest(
                InstanceId: instanceId,
                CorrelationId: Guid.NewGuid().ToString(),
                IncludeSecrets: includeSecrets),
            cancellationToken);

        if (result.IsFailure)
            return Result.Failure<ConfigurationViewModel>(result.Error);

        var viewModel = MapToViewModel(instanceId, result.Value.Configuration);
        return Result<ConfigurationViewModel>.Success(viewModel);
    }

    /// <summary>
    /// Validates configuration settings without applying them.
    /// </summary>
    public async Task<Result<ConfigurationViewModel>> ValidateConfigurationAsync(
        string instanceId,
        Dictionary<string, string> settings,
        CancellationToken cancellationToken = default)
    {
        // Note: Validation-only handler not currently in the use cases
        // For now, we'll create a view model with validation state
        await Task.CompletedTask;

        var viewModel = new ConfigurationViewModel
        {
            InstanceId = instanceId,
            Settings = settings,
            IsValid = true,
            ValidationErrors = new List<string>(),
            ValidationWarnings = new List<string>()
        };

        // Basic validation logic (can be enhanced)
        if (settings.TryGetValue("MaxPlayers", out var maxPlayersStr))
        {
            if (!int.TryParse(maxPlayersStr, out var maxPlayers) || maxPlayers < 1 || maxPlayers > 32)
            {
                viewModel.IsValid = false;
                viewModel.ValidationErrors.Add("MaxPlayers must be between 1 and 32");
            }
        }

        return Result<ConfigurationViewModel>.Success(viewModel);
    }

    /// <summary>
    /// Applies configuration changes to an instance.
    /// </summary>
    public async Task<Result<ApplyConfigurationResult>> ApplyConfigurationAsync(
        string instanceId,
        Dictionary<string, string> settings,
        bool createBackup = true,
        bool autoRestart = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _applyConfigurationHandler.Handle(
            new ApplyConfigurationRequest(
                InstanceId: instanceId,
                CorrelationId: Guid.NewGuid().ToString(),
                ConfigurationSettings: settings,
                RestartInstance: autoRestart),
            cancellationToken);

        if (result.IsFailure)
            return Result.Failure<ApplyConfigurationResult>(result.Error);

        var applyResult = new ApplyConfigurationResult
        {
            Success = result.Value.Success,
            InstanceId = result.Value.InstanceId,
            ChangedSettings = result.Value.ChangedSettings.ToList(),
            RequiresRestart = result.Value.RequiresRestart,
            WasRestarted = result.Value.WasRestarted,
            BackupCreated = result.Value.BackupCreated,
            AppliedAt = result.Value.AppliedAt,
            Message = result.Value.Message
        };

        return Result<ApplyConfigurationResult>.Success(applyResult);
    }

    // Mapping methods
    private static ConfigurationViewModel MapToViewModel(string instanceId, ConfigurationDto dto)
    {
        var settings = new Dictionary<string, string>
        {
            ["SessionName"] = dto.SessionName,
            ["ServerPassword"] = dto.ServerPassword,
            ["MaxPlayers"] = dto.MaxPlayers.ToString(),
            ["ServerMap"] = dto.ServerMap
        };

        // Add custom settings
        foreach (var kvp in dto.CustomSettings)
        {
            settings[kvp.Key] = kvp.Value;
        }

        return new ConfigurationViewModel
        {
            InstanceId = instanceId,
            Settings = settings,
            LastModified = DateTimeOffset.UtcNow,
            HasUnsavedChanges = false,
            IsValid = true
        };
    }
}

/// <summary>
/// Result of applying configuration changes.
/// </summary>
public class ApplyConfigurationResult
{
    public bool Success { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public List<string> ChangedSettings { get; set; } = new();
    public bool RequiresRestart { get; set; }
    public bool WasRestarted { get; set; }
    public bool BackupCreated { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
    public string? Message { get; set; }
}
