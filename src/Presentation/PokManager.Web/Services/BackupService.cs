using PokManager.Application.Models;
using PokManager.Application.UseCases.BackupManagement.CreateBackup;
using PokManager.Application.UseCases.BackupManagement.ListBackups;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;
using PokManager.Web.Models;

namespace PokManager.Web.Services;

/// <summary>
/// Facade service for backup operations, bridging UI layer and Application handlers.
/// </summary>
public class BackupService
{
    private readonly ListBackupsHandler _listBackupsHandler;
    private readonly CreateBackupHandler _createBackupHandler;

    public BackupService(
        ListBackupsHandler listBackupsHandler,
        CreateBackupHandler createBackupHandler)
    {
        _listBackupsHandler = listBackupsHandler ?? throw new ArgumentNullException(nameof(listBackupsHandler));
        _createBackupHandler = createBackupHandler ?? throw new ArgumentNullException(nameof(createBackupHandler));
    }

    /// <summary>
    /// Retrieves all backups across all instances.
    /// </summary>
    public async Task<Result<List<BackupViewModel>>> GetAllBackupsAsync(
        CancellationToken cancellationToken = default)
    {
        // Note: Current implementation doesn't have a "list all backups" handler
        // This would need to iterate through instances or be implemented differently
        return Result<List<BackupViewModel>>.Success(new List<BackupViewModel>());
    }

    /// <summary>
    /// Retrieves all backups for a specific instance.
    /// </summary>
    public async Task<Result<List<BackupViewModel>>> GetBackupsForInstanceAsync(
        string instanceId,
        bool includeMetadata = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _listBackupsHandler.Handle(
            new ListBackupsRequest(
                InstanceId: instanceId,
                CorrelationId: Guid.NewGuid().ToString(),
                IncludeMetadata: includeMetadata),
            cancellationToken);

        if (result.IsFailure)
            return Result.Failure<List<BackupViewModel>>(result.Error);

        var viewModels = result.Value.Backups
            .Select(MapToViewModel)
            .ToList();

        return Result<List<BackupViewModel>>.Success(viewModels);
    }

    /// <summary>
    /// Creates a new backup for an instance.
    /// </summary>
    public async Task<Result<string>> CreateBackupAsync(
        string instanceId,
        string? description = null,
        CompressionFormat compressionFormat = CompressionFormat.Gzip,
        CancellationToken cancellationToken = default)
    {
        var options = new CreateBackupOptions(
            Description: description,
            CompressionFormat: compressionFormat,
            IncludeConfiguration: true,
            IncludeLogs: false);

        var result = await _createBackupHandler.Handle(
            new CreateBackupRequest(
                InstanceId: instanceId,
                CorrelationId: Guid.NewGuid().ToString(),
                Options: options),
            cancellationToken);

        if (result.IsFailure)
            return Result.Failure<string>(result.Error);

        return Result<string>.Success(result.Value.BackupId);
    }

    /// <summary>
    /// Restores a backup (placeholder - handler not implemented in current scope).
    /// </summary>
    public async Task<Result<string>> RestoreBackupAsync(
        string instanceId,
        string backupId,
        bool stopBeforeRestore = true,
        CancellationToken cancellationToken = default)
    {
        // Note: RestoreBackupHandler would need to be implemented and injected
        await Task.CompletedTask;
        return Result.Failure<string>("RestoreBackup not yet implemented");
    }

    /// <summary>
    /// Deletes a backup (placeholder - handler not implemented in current scope).
    /// </summary>
    public async Task<Result<Unit>> DeleteBackupAsync(
        string instanceId,
        string backupId,
        CancellationToken cancellationToken = default)
    {
        // Note: DeleteBackupHandler would need to be implemented and injected
        await Task.CompletedTask;
        return Result.Failure<Unit>("DeleteBackup not yet implemented");
    }

    /// <summary>
    /// Downloads a backup file (placeholder - handler not implemented in current scope).
    /// </summary>
    public async Task<Result<Stream>> DownloadBackupAsync(
        string instanceId,
        string backupId,
        CancellationToken cancellationToken = default)
    {
        // Note: This would interact with the backup store directly or through a handler
        await Task.CompletedTask;
        return Result.Failure<Stream>("DownloadBackup not yet implemented");
    }

    // Mapping methods
    private static BackupViewModel MapToViewModel(BackupSummaryDto dto)
    {
        return new BackupViewModel
        {
            BackupId = dto.BackupId,
            InstanceId = dto.InstanceId,
            InstanceName = dto.InstanceId, // Using ID as name for now
            CreatedAt = dto.CreatedAt,
            CompressionFormat = MapCompressionFormat(dto.CompressionFormat),
            SizeBytes = dto.FileSizeBytes ?? 0,
            IsAutomatic = false // This info not available in current DTO
        };
    }

    private static CompressionFormat MapCompressionFormat(CompressionFormat format)
    {
        // Direct mapping since BackupViewModel now uses Domain.CompressionFormat
        return format;
    }
}
