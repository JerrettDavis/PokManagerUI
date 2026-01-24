using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager restore commands.
/// </summary>
public sealed class RestoreCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;
    private InstanceId? _instanceId;
    private BackupId? _backupId;

    private RestoreCommandBuilder(string scriptPath)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand("restore");
    }

    /// <summary>
    /// Creates a new restore command builder with the specified script path.
    /// </summary>
    public static RestoreCommandBuilder Create(string scriptPath)
    {
        return new RestoreCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the instance to restore. Required for restore command.
    /// </summary>
    public RestoreCommandBuilder ForInstance(InstanceId instanceId)
    {
        _instanceId = instanceId;
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Sets the backup to restore from. Required for restore command.
    /// </summary>
    public RestoreCommandBuilder FromBackup(BackupId backupId)
    {
        _backupId = backupId;
        return this;
    }

    /// <summary>
    /// Forces restore even if instance is running or conflicts exist.
    /// </summary>
    public RestoreCommandBuilder WithForce()
    {
        _baseBuilder.WithFlag("force");
        return this;
    }

    /// <summary>
    /// Stops the instance before restoring.
    /// </summary>
    public RestoreCommandBuilder WithStopBeforeRestore()
    {
        _baseBuilder.WithFlag("stop-before-restore");
        return this;
    }

    /// <summary>
    /// Starts the instance after restoring.
    /// </summary>
    public RestoreCommandBuilder WithStartAfterRestore()
    {
        _baseBuilder.WithFlag("start-after-restore");
        return this;
    }

    /// <summary>
    /// Builds the final restore command.
    /// </summary>
    public Result<string> Build()
    {
        // Validate instance ID is provided
        if (_instanceId == null)
        {
            return Result.Failure<string>("Instance ID is required for restore command.");
        }

        // Validate backup ID is provided
        if (_backupId == null)
        {
            return Result.Failure<string>("Backup ID is required for restore command.");
        }

        // Add backup ID argument
        _baseBuilder.WithArgument("backup-id", _backupId.Value);

        return _baseBuilder.Build();
    }
}
