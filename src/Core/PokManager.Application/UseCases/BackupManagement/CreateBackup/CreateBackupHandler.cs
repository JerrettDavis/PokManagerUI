using FluentValidation;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.BackupManagement.CreateBackup;

/// <summary>
/// Handler for creating backups of Palworld server instances.
/// Orchestrates the backup creation process including validation, locking, execution, storage, and auditing.
/// </summary>
public class CreateBackupHandler(
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
    private readonly CreateBackupRequestValidator _validator = new();

    public async Task<Result<CreateBackupResponse>> Handle(
        CreateBackupRequest request,
        CancellationToken cancellationToken)
    {
        var startTime = _clock.UtcNow;

        // 1. Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<CreateBackupResponse>(errors);
        }

        IOperationLock? operationLock = null;

        try
        {
            // 2. Acquire operation lock
            var lockResult = await _lockManager.AcquireLockAsync(
                request.InstanceId,
                request.CorrelationId,
                TimeSpan.FromMinutes(5),
                cancellationToken
            );

            if (lockResult.IsFailure)
            {
                return Result.Failure<CreateBackupResponse>(
                    $"Cannot create backup: Instance {request.InstanceId} is locked by another operation"
                );
            }

            operationLock = lockResult.Value;

            // 3. Call PokManagerClient to create the backup
            var createBackupResult = await _pokManagerClient.CreateBackupAsync(
                request.InstanceId,
                request.Options,
                cancellationToken
            );

            if (createBackupResult.IsFailure)
            {
                // Create failure audit event
                await CreateAuditEvent(
                    request.InstanceId,
                    "CreateBackup",
                    "Failure",
                    startTime,
                    request.Options?.Description,
                    createBackupResult.Error
                );

                return Result.Failure<CreateBackupResponse>(createBackupResult.Error);
            }

            var backupId = createBackupResult.Value;

            // 4. Retrieve the created backup info from PokManagerClient
            var listBackupsResult = await _pokManagerClient.ListBackupsAsync(
                request.InstanceId,
                cancellationToken
            );

            BackupInfo? backupInfo = null;
            if (listBackupsResult.IsSuccess)
            {
                backupInfo = listBackupsResult.Value.FirstOrDefault(b => b.BackupId == backupId);
            }

            // 5. Store backup metadata in IBackupStore
            if (backupInfo != null)
            {
                var storeResult = await _backupStore.StoreBackupAsync(backupInfo, cancellationToken);
                if (storeResult.IsFailure)
                {
                    // Log warning but don't fail the operation since backup was created
                    // In production, this would be logged properly
                }
            }
            else
            {
                // If we couldn't retrieve the backup info, create a minimal record
                backupInfo = new BackupInfo(
                    BackupId: backupId,
                    InstanceId: request.InstanceId,
                    Description: request.Options?.Description,
                    CompressionFormat: request.Options?.CompressionFormat ?? Domain.Enumerations.CompressionFormat.Gzip,
                    SizeInBytes: 0,
                    CreatedAt: _clock.UtcNow,
                    FilePath: string.Empty,
                    IsAutomatic: false,
                    ServerVersion: null
                );

                await _backupStore.StoreBackupAsync(backupInfo, cancellationToken);
            }

            // 6. Create success audit event
            var duration = _clock.UtcNow - startTime;
            await CreateAuditEvent(
                request.InstanceId,
                "CreateBackup",
                "Success",
                startTime,
                request.Options?.Description,
                errorMessage: null,
                duration: duration
            );

            // 7. Invalidate backup cache and trigger refresh
            await _cacheInvalidation.InvalidateBackupsAsync(request.InstanceId, cancellationToken);

            // 8. Return success response
            var response = new CreateBackupResponse(
                Success: true,
                BackupId: backupId,
                InstanceId: request.InstanceId,
                FilePath: backupInfo.FilePath,
                SizeInBytes: backupInfo.SizeInBytes,
                CreatedAt: backupInfo.CreatedAt,
                Duration: duration
            );

            return Result<CreateBackupResponse>.Success(response);
        }
        finally
        {
            // 8. Always release the lock
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
        string? description,
        string? errorMessage = null,
        TimeSpan? duration = null)
    {
        var details = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(description))
        {
            details["Description"] = description;
        }

        var auditEvent = new AuditEvent(
            EventId: Guid.NewGuid(),
            InstanceId: instanceId,
            OperationType: operationType,
            PerformedBy: "System",
            PerformedAt: performedAt,
            Outcome: outcome,
            Duration: duration,
            Details: details.Count > 0 ? details : null,
            ErrorMessage: errorMessage
        );

        await _auditSink.EmitAsync(auditEvent, CancellationToken.None);
    }
}
