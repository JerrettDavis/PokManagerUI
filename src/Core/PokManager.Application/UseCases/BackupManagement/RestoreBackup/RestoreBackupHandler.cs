using FluentValidation;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.BackupManagement.RestoreBackup;

/// <summary>
/// Handler for restoring backups to Palworld server instances.
/// Orchestrates the restore process including validation, safety backup creation, stopping the instance,
/// restoring the backup, and optionally restarting the instance.
/// </summary>
public class RestoreBackupHandler(
    IPokManagerClient pokManagerClient,
    IBackupStore backupStore,
    IOperationLockManager lockManager,
    IAuditSink auditSink,
    IClock clock,
    ICacheInvalidationService cacheInvalidation
)
{
    private readonly IPokManagerClient _pokManagerClient = pokManagerClient ?? throw new ArgumentNullException(nameof(pokManagerClient));
    private readonly IBackupStore _backupStore = backupStore ?? throw new ArgumentNullException(nameof(backupStore));
    private readonly IOperationLockManager _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
    private readonly IAuditSink _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly ICacheInvalidationService _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
    private readonly RestoreBackupRequestValidator _validator = new();

    public async Task<Result<RestoreBackupResponse>> Handle(
        RestoreBackupRequest request,
        CancellationToken cancellationToken)
    {
        var startTime = _clock.UtcNow;
        string? safetyBackupId = null;

        // 1. Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<RestoreBackupResponse>(errors);
        }

        IOperationLock? operationLock = null;

        try
        {
            // 2. Acquire operation lock
            var lockResult = await _lockManager.AcquireLockAsync(
                request.InstanceId,
                request.CorrelationId,
                TimeSpan.FromMinutes(10),
                cancellationToken
            );

            if (lockResult.IsFailure)
            {
                return Result.Failure<RestoreBackupResponse>(
                    $"Cannot restore backup: Instance {request.InstanceId} is locked by another operation"
                );
            }

            operationLock = lockResult.Value;

            // 3. Get instance status to check if it's running
            var statusResult = await _pokManagerClient.GetInstanceStatusAsync(
                request.InstanceId,
                cancellationToken
            );

            if (statusResult.IsFailure)
            {
                await CreateAuditEvent(
                    request.InstanceId,
                    "RestoreBackup",
                    "Failure",
                    startTime,
                    request.BackupId,
                    $"Failed to get instance status: {statusResult.Error}"
                );

                return Result.Failure<RestoreBackupResponse>(
                    $"Failed to get instance status: {statusResult.Error}"
                );
            }

            var instanceStatus = statusResult.Value;
            var wasRunning = instanceStatus.State == Domain.Enumerations.InstanceState.Running;

            // 4. Create safety backup if requested
            if (request.CreateSafetyBackup)
            {
                var safetyBackupOptions = new CreateBackupOptions(
                    Description: $"Safety backup before restoring {request.BackupId}",
                    CompressionFormat: Domain.Enumerations.CompressionFormat.Gzip
                );

                var safetyBackupResult = await _pokManagerClient.CreateBackupAsync(
                    request.InstanceId,
                    safetyBackupOptions,
                    cancellationToken
                );

                if (safetyBackupResult.IsFailure)
                {
                    await CreateAuditEvent(
                        request.InstanceId,
                        "RestoreBackup",
                        "Failure",
                        startTime,
                        request.BackupId,
                        $"Failed to create safety backup: {safetyBackupResult.Error}"
                    );

                    return Result.Failure<RestoreBackupResponse>(
                        $"Failed to create safety backup: {safetyBackupResult.Error}"
                    );
                }

                safetyBackupId = safetyBackupResult.Value;

                // Store safety backup metadata
                var listBackupsResult = await _pokManagerClient.ListBackupsAsync(
                    request.InstanceId,
                    cancellationToken
                );

                if (listBackupsResult.IsSuccess)
                {
                    var safetyBackupInfo = listBackupsResult.Value.FirstOrDefault(b => b.BackupId == safetyBackupId);
                    if (safetyBackupInfo != null)
                    {
                        await _backupStore.StoreBackupAsync(safetyBackupInfo, cancellationToken);
                    }
                }
            }

            // 5. Restore the backup using IPokManagerClient
            var restoreOptions = new RestoreBackupOptions(
                StopInstance: wasRunning,  // Only stop if it's running
                StartAfterRestore: false,   // We'll handle restart manually
                BackupBeforeRestore: false, // We already created safety backup
                ValidateBackup: true
            );

            var restoreResult = await _pokManagerClient.RestoreBackupAsync(
                request.InstanceId,
                request.BackupId,
                restoreOptions,
                cancellationToken
            );

            if (restoreResult.IsFailure)
            {
                await CreateAuditEvent(
                    request.InstanceId,
                    "RestoreBackup",
                    "Failure",
                    startTime,
                    request.BackupId,
                    $"Failed to restore backup: {restoreResult.Error}",
                    safetyBackupId
                );

                return Result.Failure<RestoreBackupResponse>(
                    $"Failed to restore backup: {restoreResult.Error}"
                );
            }

            // 6. Start instance if it was running before
            if (wasRunning)
            {
                var startResult = await _pokManagerClient.StartInstanceAsync(
                    request.InstanceId,
                    cancellationToken
                );

                if (startResult.IsFailure)
                {
                    // Log warning but don't fail the operation since restore succeeded
                    await CreateAuditEvent(
                        request.InstanceId,
                        "RestoreBackup",
                        "Warning",
                        startTime,
                        request.BackupId,
                        $"Backup restored successfully but failed to restart instance: {startResult.Error}",
                        safetyBackupId
                    );

                    var duration = _clock.UtcNow - startTime;

                    return Result<RestoreBackupResponse>.Success(new RestoreBackupResponse(
                        Success: true,
                        BackupId: request.BackupId,
                        InstanceId: request.InstanceId,
                        SafetyBackupId: safetyBackupId,
                        Duration: duration,
                        Message: $"Backup restored successfully but instance failed to restart: {startResult.Error}"
                    ));
                }
            }

            // 7. Create success audit event
            var finalDuration = _clock.UtcNow - startTime;
            await CreateAuditEvent(
                request.InstanceId,
                "RestoreBackup",
                "Success",
                startTime,
                request.BackupId,
                errorMessage: null,
                safetyBackupId: safetyBackupId,
                duration: finalDuration
            );

            // 8. Invalidate cache and trigger refresh
            await _cacheInvalidation.InvalidateInstanceAsync(request.InstanceId, cancellationToken);
            await _cacheInvalidation.InvalidateBackupsAsync(request.InstanceId, cancellationToken);

            // 9. Return success response
            var response = new RestoreBackupResponse(
                Success: true,
                BackupId: request.BackupId,
                InstanceId: request.InstanceId,
                SafetyBackupId: safetyBackupId,
                Duration: finalDuration,
                Message: wasRunning ? "Backup restored and instance restarted successfully" : "Backup restored successfully"
            );

            return Result<RestoreBackupResponse>.Success(response);
        }
        finally
        {
            // 9. Always release the lock
            if (operationLock != null)
            {
                await operationLock.DisposeAsync();
            }
        }
    }

    private async Task CreateAuditEvent(
        string instanceId,
        string operationType,
        string outcome,
        DateTimeOffset performedAt,
        string backupId,
        string? errorMessage = null,
        string? safetyBackupId = null,
        TimeSpan? duration = null)
    {
        var details = new Dictionary<string, string>
        {
            ["BackupId"] = backupId
        };

        if (!string.IsNullOrWhiteSpace(safetyBackupId))
        {
            details["SafetyBackupId"] = safetyBackupId;
        }

        var auditEvent = new AuditEvent(
            EventId: Guid.NewGuid(),
            InstanceId: instanceId,
            OperationType: operationType,
            PerformedBy: "System",
            PerformedAt: performedAt,
            Outcome: outcome,
            Duration: duration,
            Details: details,
            ErrorMessage: errorMessage
        );

        await _auditSink.EmitAsync(auditEvent, CancellationToken.None);
    }
}
