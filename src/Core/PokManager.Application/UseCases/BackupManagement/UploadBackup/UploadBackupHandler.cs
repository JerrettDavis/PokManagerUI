using FluentValidation;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Application.UseCases.BackupManagement.UploadBackup;

/// <summary>
/// Handler for uploading backup files from external sources.
/// Saves the uploaded backup and optionally restores it immediately.
/// </summary>
public class UploadBackupHandler(
    IBackupStore backupStore,
    IOperationLockManager lockManager,
    IAuditSink auditSink,
    IClock clock,
    ICacheInvalidationService cacheInvalidation
)
{
    private readonly IBackupStore _backupStore = backupStore ?? throw new ArgumentNullException(nameof(backupStore));
    private readonly IOperationLockManager _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
    private readonly IAuditSink _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly ICacheInvalidationService _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
    private readonly UploadBackupRequestValidator _validator = new();

    public async Task<Result<UploadBackupResponse>> Handle(
        UploadBackupRequest request,
        CancellationToken cancellationToken)
    {
        var startTime = _clock.UtcNow;

        // 1. Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result.Failure<UploadBackupResponse>(errors);
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
                return Result.Failure<UploadBackupResponse>(
                    $"Cannot upload backup: Instance {request.InstanceId} is locked by another operation"
                );
            }

            operationLock = lockResult.Value;

            // 3. Create backup directory if it doesn't exist
            var backupDir = Path.Combine("/opt/pok/instances/backups", request.InstanceId);
            Directory.CreateDirectory(backupDir);

            // 4. Generate backup ID and file path
            var backupId = $"upload-{_clock.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
            var fileName = $"{backupId}{Path.GetExtension(request.FileName)}";
            var filePath = Path.Combine(backupDir, fileName);

            // 5. Save the uploaded file
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await request.FileStream.CopyToAsync(fileStream, cancellationToken);
            }

            // 6. Get file size
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            // 7. Determine compression format from file extension
            var compressionFormat = request.FileName.EndsWith(".tar.gz") || request.FileName.EndsWith(".gz") || request.FileName.EndsWith(".zip")
                ? CompressionFormat.Gzip
                : CompressionFormat.Gzip; // Default

            // 8. Create BackupInfo
            var backupInfo = new BackupInfo(
                BackupId: backupId,
                InstanceId: request.InstanceId,
                Description: request.Description ?? $"Uploaded backup: {request.FileName}",
                CompressionFormat: compressionFormat,
                SizeInBytes: fileSize,
                CreatedAt: _clock.UtcNow,
                FilePath: filePath,
                IsAutomatic: false,
                ServerVersion: null
            );

            // 9. Store backup metadata
            var storeResult = await _backupStore.StoreBackupAsync(backupInfo, cancellationToken);
            if (storeResult.IsFailure)
            {
                // Clean up the file
                try { File.Delete(filePath); } catch { /* Ignore cleanup errors */ }

                await CreateAuditEvent(
                    request.InstanceId,
                    "UploadBackup",
                    "Failure",
                    startTime,
                    backupId,
                    $"Failed to store backup metadata: {storeResult.Error}"
                );

                return Result.Failure<UploadBackupResponse>(
                    $"Failed to store backup metadata: {storeResult.Error}"
                );
            }

            // 10. Create success audit event
            var duration = _clock.UtcNow - startTime;
            await CreateAuditEvent(
                request.InstanceId,
                "UploadBackup",
                "Success",
                startTime,
                backupId,
                errorMessage: null,
                duration: duration
            );

            // 11. Invalidate backup cache and trigger refresh
            await _cacheInvalidation.InvalidateBackupsAsync(request.InstanceId, cancellationToken);

            // 12. Return success response
            var response = new UploadBackupResponse(
                Success: true,
                BackupId: backupId,
                InstanceId: request.InstanceId,
                FilePath: filePath,
                SizeInBytes: fileSize,
                Restored: false,
                Message: $"Backup uploaded successfully. Use the restore endpoint to restore this backup."
            );

            return Result<UploadBackupResponse>.Success(response);
        }
        finally
        {
            // 12. Always release the lock
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
        TimeSpan? duration = null)
    {
        var details = new Dictionary<string, string>
        {
            ["BackupId"] = backupId
        };

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
