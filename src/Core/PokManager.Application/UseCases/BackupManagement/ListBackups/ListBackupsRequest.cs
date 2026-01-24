namespace PokManager.Application.UseCases.BackupManagement.ListBackups;

/// <summary>
/// Request to list all backups for a specific instance.
/// </summary>
/// <param name="InstanceId">The unique identifier of the instance.</param>
/// <param name="CorrelationId">Unique identifier for tracking the request.</param>
/// <param name="IncludeMetadata">Whether to include metadata like file size in the response.</param>
public record ListBackupsRequest(
    string InstanceId,
    string CorrelationId,
    bool IncludeMetadata = false);
