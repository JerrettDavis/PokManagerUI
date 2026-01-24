using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Application.UseCases.BackupManagement.CreateBackup;
using PokManager.Application.UseCases.Configuration.ApplyConfiguration;
using PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Application.UseCases.ConfigurationTemplates.ApplyTemplate;

/// <summary>
/// Handler for applying a configuration template to an instance.
/// Supports automatic backup creation, partial template merging, and restart handling.
/// </summary>
public class ApplyTemplateHandler
{
    private readonly IConfigurationTemplateStore _templateStore;
    private readonly GetConfigurationHandler _getConfigHandler;
    private readonly ApplyConfigurationHandler _applyConfigHandler;
    private readonly CreateBackupHandler _createBackupHandler;
    private readonly IAuditSink _auditSink;
    private readonly IClock _clock;
    private readonly ApplyTemplateValidator _validator;

    public ApplyTemplateHandler(
        IConfigurationTemplateStore templateStore,
        GetConfigurationHandler getConfigHandler,
        ApplyConfigurationHandler applyConfigHandler,
        CreateBackupHandler createBackupHandler,
        IAuditSink auditSink,
        IClock clock)
    {
        _templateStore = templateStore;
        _getConfigHandler = getConfigHandler;
        _applyConfigHandler = applyConfigHandler;
        _createBackupHandler = createBackupHandler;
        _auditSink = auditSink;
        _clock = clock;
        _validator = new ApplyTemplateValidator();
    }

    public async Task<Result<ApplyTemplateResponse>> Handle(
        ApplyTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var startTime = _clock.UtcNow;

        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<ApplyTemplateResponse>(
                $"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        // Get template
        var templateResult = await _templateStore.GetTemplateAsync(request.TemplateId, cancellationToken);
        if (templateResult.IsFailure)
            return Result.Failure<ApplyTemplateResponse>(templateResult.Error);

        var template = templateResult.Value;

        // Get current instance configuration
        var currentConfigResult = await _getConfigHandler.Handle(
            new GetConfigurationRequest(request.CorrelationId, request.InstanceId, IncludeSecrets: true),
            cancellationToken);

        if (currentConfigResult.IsFailure)
            return Result.Failure<ApplyTemplateResponse>(currentConfigResult.Error);

        var currentConfig = currentConfigResult.Value.Configuration;
        var currentConfigDict = BuildConfigurationDictionary(currentConfig);

        // Create backup if requested
        string? backupId = null;
        bool backupCreated = false;

        if (request.CreateBackup)
        {
            var backupRequest = new CreateBackupRequest(
                request.InstanceId,
                request.CorrelationId,
                new CreateBackupOptions(
                    Description: $"Automatic backup before applying template: {template.Name}",
                    CompressionFormat: CompressionFormat.Zstd
                )
            );

            var backupResult = await _createBackupHandler.Handle(backupRequest, cancellationToken);
            if (backupResult.IsFailure)
            {
                return Result.Failure<ApplyTemplateResponse>(
                    $"Failed to create backup before applying template: {backupResult.Error}");
            }

            backupId = backupResult.Value.BackupId;
            backupCreated = true;
        }

        // Merge template settings with current configuration
        var mergedConfig = MergeConfigurations(currentConfigDict, template);

        // Apply configuration
        var applyRequest = new ApplyConfigurationRequest(
            request.InstanceId,
            request.CorrelationId,
            new Dictionary<string, string>(mergedConfig),
            request.RestartIfNeeded
        );

        var applyResult = await _applyConfigHandler.Handle(applyRequest, cancellationToken);
        if (applyResult.IsFailure)
        {
            return Result.Failure<ApplyTemplateResponse>(
                $"Failed to apply configuration: {applyResult.Error}");
        }

        var applyResponse = applyResult.Value;

        // Increment template usage count
        await _templateStore.IncrementUsageCountAsync(template.TemplateId, cancellationToken);

        // Create audit event
        await _auditSink.EmitAsync(new AuditEvent(
            Guid.NewGuid(),
            request.InstanceId,
            "TemplateApplied",
            "System",
            _clock.UtcNow,
            "Success",
            null,
            new Dictionary<string, string>
            {
                ["TemplateId"] = template.TemplateId,
                ["TemplateName"] = template.Name,
                ["TemplateType"] = template.Type.ToString(),
                ["IsPartial"] = template.IsPartial.ToString(),
                ["BackupCreated"] = backupCreated.ToString(),
                ["BackupId"] = backupId ?? "",
                ["ChangedSettings"] = string.Join(", ", applyResponse.ChangedSettings),
                ["RequiresRestart"] = applyResponse.RequiresRestart.ToString(),
                ["WasRestarted"] = applyResponse.WasRestarted.ToString(),
                ["CorrelationId"] = request.CorrelationId
            },
            null
        ), cancellationToken);

        return Result<ApplyTemplateResponse>.Success(new ApplyTemplateResponse(
            applyResponse.Success,
            template.TemplateId,
            template.Name,
            request.InstanceId,
            applyResponse.ChangedSettings.ToArray(),
            backupCreated,
            backupId,
            applyResponse.RequiresRestart,
            applyResponse.WasRestarted,
            _clock.UtcNow,
            $"Successfully applied template '{template.Name}' to instance. " +
            $"{(backupCreated ? $"Backup created: {backupId}. " : "")}" +
            $"{applyResponse.Message}"
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

    private IReadOnlyDictionary<string, string> MergeConfigurations(
        Dictionary<string, string> currentConfig,
        ConfigurationTemplateInfo template)
    {
        // Start with current configuration
        var merged = new Dictionary<string, string>(currentConfig, StringComparer.OrdinalIgnoreCase);

        // For partial templates, only apply included settings
        var settingsToApply = template.IsPartial && template.IncludedSettings.Length > 0
            ? template.ConfigurationData.Where(kvp => template.IncludedSettings.Contains(kvp.Key))
            : template.ConfigurationData;

        // Apply template settings (overwriting current values)
        foreach (var (key, value) in settingsToApply)
        {
            merged[key] = value;
        }

        return merged;
    }
}
