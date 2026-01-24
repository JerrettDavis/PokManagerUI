using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager backup commands.
/// </summary>
public sealed class BackupCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;
    private InstanceId? _instanceId;

    private BackupCommandBuilder(string scriptPath)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand("backup");
    }

    /// <summary>
    /// Creates a new backup command builder with the specified script path.
    /// </summary>
    public static BackupCommandBuilder Create(string scriptPath)
    {
        return new BackupCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the instance to backup. Required for backup command.
    /// </summary>
    public BackupCommandBuilder ForInstance(InstanceId instanceId)
    {
        _instanceId = instanceId;
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Sets the compression format (gzip, bzip2, xz, zstd, none).
    /// </summary>
    public BackupCommandBuilder WithCompression(string format)
    {
        _baseBuilder.WithArgument("compress", format);
        return this;
    }

    /// <summary>
    /// Sets the output path for the backup file.
    /// </summary>
    public BackupCommandBuilder WithOutputPath(string path)
    {
        _baseBuilder.WithArgument("output", path);
        return this;
    }

    /// <summary>
    /// Creates an incremental backup (only changed files).
    /// </summary>
    public BackupCommandBuilder WithIncremental()
    {
        _baseBuilder.WithFlag("incremental");
        return this;
    }

    /// <summary>
    /// Excludes log files from the backup.
    /// </summary>
    public BackupCommandBuilder WithExcludeLogs()
    {
        _baseBuilder.WithFlag("exclude-logs");
        return this;
    }

    /// <summary>
    /// Sets a description for the backup.
    /// </summary>
    public BackupCommandBuilder WithDescription(string description)
    {
        _baseBuilder.WithArgument("description", description);
        return this;
    }

    /// <summary>
    /// Builds the final backup command.
    /// </summary>
    public Result<string> Build()
    {
        // Validate instance ID is provided
        if (_instanceId == null)
        {
            return Result.Failure<string>("Instance ID is required for backup command.");
        }

        return _baseBuilder.Build();
    }
}
