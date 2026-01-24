using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.BackupManagement.ListBackups;

/// <summary>
/// Handler for listing backups for a specific instance.
/// </summary>
public class ListBackupsHandler
{
    private readonly IBackupStore _backupStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListBackupsHandler"/> class.
    /// </summary>
    /// <param name="backupStore">The backup store for retrieving backup information.</param>
    public ListBackupsHandler(IBackupStore backupStore)
    {
        _backupStore = backupStore ?? throw new ArgumentNullException(nameof(backupStore));
    }

    /// <summary>
    /// Handles the request to list backups for an instance.
    /// </summary>
    /// <param name="request">The list backups request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of backup summaries or an error.</returns>
    public async Task<Result<ListBackupsResponse>> Handle(
        ListBackupsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Retrieve backups from the store
        var storeResult = await _backupStore.ListBackupsAsync(request.InstanceId, cancellationToken);

        if (storeResult.IsFailure)
            return Result.Failure<ListBackupsResponse>(storeResult.Error);

        // Map BackupInfo to BackupSummaryDto
        var backupSummaries = storeResult.Value
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BackupSummaryDto(
                BackupId: b.BackupId,
                InstanceId: b.InstanceId,
                CreatedAt: b.CreatedAt,
                CompressionFormat: b.CompressionFormat,
                FileSizeBytes: request.IncludeMetadata ? b.SizeInBytes : null
            ))
            .ToList();

        var response = new ListBackupsResponse(backupSummaries);

        return Result<ListBackupsResponse>.Success(response);
    }
}
