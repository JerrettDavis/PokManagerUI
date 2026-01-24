using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager update commands.
/// </summary>
public sealed class UpdateCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;

    private UpdateCommandBuilder(string scriptPath)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand("update");
    }

    /// <summary>
    /// Creates a new update command builder with the specified script path.
    /// </summary>
    public static UpdateCommandBuilder Create(string scriptPath)
    {
        return new UpdateCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the instance to update. If not called, updates all instances or the POK Manager itself.
    /// </summary>
    public UpdateCommandBuilder ForInstance(InstanceId instanceId)
    {
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Sets the target version to update to.
    /// </summary>
    public UpdateCommandBuilder ToVersion(string version)
    {
        _baseBuilder.WithArgument("version", version);
        return this;
    }

    /// <summary>
    /// Creates a backup before performing the update.
    /// </summary>
    public UpdateCommandBuilder WithBackupBeforeUpdate()
    {
        _baseBuilder.WithFlag("backup-before-update");
        return this;
    }

    /// <summary>
    /// Validates the update before applying it.
    /// </summary>
    public UpdateCommandBuilder WithValidate()
    {
        _baseBuilder.WithFlag("validate");
        return this;
    }

    /// <summary>
    /// Restarts the instance after the update completes.
    /// </summary>
    public UpdateCommandBuilder WithRestartAfterUpdate()
    {
        _baseBuilder.WithFlag("restart-after-update");
        return this;
    }

    /// <summary>
    /// Forces the update even if there are warnings or conflicts.
    /// </summary>
    public UpdateCommandBuilder WithForce()
    {
        _baseBuilder.WithFlag("force");
        return this;
    }

    /// <summary>
    /// Builds the final update command.
    /// </summary>
    public Result<string> Build()
    {
        return _baseBuilder.Build();
    }
}
