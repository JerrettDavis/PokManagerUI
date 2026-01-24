using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.Configuration.ApplyConfiguration;

/// <summary>
/// Handler for applying configuration changes to a Palworld server instance.
/// </summary>
public class ApplyConfigurationHandler(
    IPokManagerClient client,
    IOperationLockManager lockManager,
    IAuditSink auditSink,
    IClock clock
)
{
    private readonly IPokManagerClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly IOperationLockManager _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
    private readonly IAuditSink _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly ApplyConfigurationRequestValidator _validator = new();

    /// <summary>
    /// Handles the ApplyConfiguration request.
    /// </summary>
    /// <param name="request">The request containing the instance ID and configuration settings.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the configuration response or an error.</returns>
    public async Task<Result<ApplyConfigurationResponse>> Handle(
        ApplyConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var startTime = _clock.UtcNow;

        // Validate the request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<ApplyConfigurationResponse>(errors);
        }

        // Try to acquire lock
        var lockResult = await _lockManager.AcquireLockAsync(
            request.InstanceId,
            request.CorrelationId,
            TimeSpan.FromMinutes(5),
            cancellationToken);

        if (lockResult.IsFailure)
        {
            return Result.Failure<ApplyConfigurationResponse>(lockResult.Error);
        }

        var operationLock = lockResult.Value;

        try
        {
            // Optional: Validate configuration before applying
            var validateOptions = new ApplyConfigurationOptions(
                ValidateBeforeApply: true,
                BackupBeforeApply: true,
                RestartIfNeeded: request.RestartInstance,
                DryRun: false);

            if (validateOptions.ValidateBeforeApply)
            {
                var configValidationResult = await _client.ValidateConfigurationAsync(
                    request.InstanceId,
                    request.ConfigurationSettings,
                    cancellationToken);

                if (configValidationResult.IsFailure)
                {
                    await CreateAuditEvent(
                        request.InstanceId,
                        startTime,
                        "Failure",
                        null,
                        configValidationResult.Error);

                    return Result.Failure<ApplyConfigurationResponse>(configValidationResult.Error);
                }

                var validation = configValidationResult.Value;
                if (!validation.IsValid)
                {
                    var validationErrors = string.Join("; ", validation.Errors);
                    await CreateAuditEvent(
                        request.InstanceId,
                        startTime,
                        "Failure",
                        null,
                        $"Validation failed: {validationErrors}");

                    return Result.Failure<ApplyConfigurationResponse>(
                        $"Validation failed: {validationErrors}");
                }
            }

            // Apply configuration via PokManagerClient
            var applyResult = await _client.ApplyConfigurationAsync(
                request.InstanceId,
                request.ConfigurationSettings,
                validateOptions,
                cancellationToken);

            if (applyResult.IsFailure)
            {
                await CreateAuditEvent(
                    request.InstanceId,
                    startTime,
                    "Failure",
                    null,
                    applyResult.Error);

                return Result.Failure<ApplyConfigurationResponse>(applyResult.Error);
            }

            var clientResult = applyResult.Value;

            // Create response
            var response = new ApplyConfigurationResponse(
                clientResult.Success,
                request.InstanceId,
                clientResult.ChangedSettings,
                clientResult.RequiredRestart,
                clientResult.WasRestarted,
                clientResult.BackupCreated,
                _clock.UtcNow,
                clientResult.Message);

            // Create success audit event
            var details = new Dictionary<string, string>
            {
                ["ChangedSettings"] = string.Join(", ", clientResult.ChangedSettings),
                ["RequiresRestart"] = clientResult.RequiredRestart.ToString(),
                ["WasRestarted"] = clientResult.WasRestarted.ToString(),
                ["BackupCreated"] = clientResult.BackupCreated.ToString()
            };

            await CreateAuditEvent(
                request.InstanceId,
                startTime,
                "Success",
                details,
                null);

            return Result<ApplyConfigurationResponse>.Success(response);
        }
        finally
        {
            // Always release the lock
            await operationLock.DisposeAsync();
        }
    }

    private async Task CreateAuditEvent(
        string instanceId,
        DateTimeOffset startTime,
        string outcome,
        IReadOnlyDictionary<string, string>? details,
        string? errorMessage)
    {
        var duration = _clock.UtcNow - startTime;

        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            instanceId,
            "ApplyConfiguration",
            "System", // In production, this would be the authenticated user
            _clock.UtcNow,
            outcome,
            duration,
            details,
            errorMessage);

        await _auditSink.EmitAsync(auditEvent, CancellationToken.None);
    }
}
