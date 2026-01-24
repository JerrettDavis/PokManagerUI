using PokManager.Domain.Enumerations;

namespace PokManager.Domain.Entities;

public class Backup
{
    public string BackupId { get; }
    public string InstanceId { get; }
    public DateTimeOffset CreatedAt { get; }
    public long SizeBytes { get; }
    public CompressionFormat CompressionFormat { get; }
    public string FilePath { get; }

    public Backup(
        string backupId,
        string instanceId,
        long sizeBytes,
        CompressionFormat compressionFormat,
        string filePath)
    {
        if (sizeBytes <= 0)
        {
            throw new ArgumentException("SizeBytes must be greater than 0", nameof(sizeBytes));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("FilePath cannot be empty", nameof(filePath));
        }

        BackupId = backupId;
        InstanceId = instanceId;
        SizeBytes = sizeBytes;
        CompressionFormat = compressionFormat;
        FilePath = filePath;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
