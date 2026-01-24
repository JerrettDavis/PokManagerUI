using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager stop commands.
/// </summary>
public sealed class StopCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;
    private InstanceId? _instanceId;
    private int? _gracePeriod;

    private StopCommandBuilder(string scriptPath)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand("stop");
    }

    /// <summary>
    /// Creates a new stop command builder with the specified script path.
    /// </summary>
    public static StopCommandBuilder Create(string scriptPath)
    {
        return new StopCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the instance to stop. Required for stop command.
    /// </summary>
    public StopCommandBuilder ForInstance(InstanceId instanceId)
    {
        _instanceId = instanceId;
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Sets the grace period in seconds before forcing shutdown.
    /// </summary>
    public StopCommandBuilder WithGracePeriod(int seconds)
    {
        _gracePeriod = seconds;
        return this;
    }

    /// <summary>
    /// Forces immediate shutdown without waiting.
    /// </summary>
    public StopCommandBuilder WithForce()
    {
        _baseBuilder.WithFlag("force");
        return this;
    }

    /// <summary>
    /// Saves the world state before stopping.
    /// </summary>
    public StopCommandBuilder WithSave()
    {
        _baseBuilder.WithFlag("save");
        return this;
    }

    /// <summary>
    /// Builds the final stop command.
    /// </summary>
    public Result<string> Build()
    {
        // Validate instance ID is provided
        if (_instanceId == null)
        {
            return Result.Failure<string>("Instance ID is required for stop command.");
        }

        // Validate grace period if provided
        if (_gracePeriod.HasValue && _gracePeriod.Value <= 0)
        {
            return Result.Failure<string>("Grace period must be a positive number.");
        }

        // Add grace period if specified
        if (_gracePeriod.HasValue)
        {
            _baseBuilder.WithArgument("grace-period", _gracePeriod.Value.ToString());
        }

        return _baseBuilder.Build();
    }
}
