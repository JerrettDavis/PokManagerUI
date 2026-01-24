using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager restart commands.
/// </summary>
public sealed class RestartCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;
    private InstanceId? _instanceId;
    private int? _gracePeriod;

    private RestartCommandBuilder(string scriptPath)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand("restart");
    }

    /// <summary>
    /// Creates a new restart command builder with the specified script path.
    /// </summary>
    public static RestartCommandBuilder Create(string scriptPath)
    {
        return new RestartCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the instance to restart. Required for restart command.
    /// </summary>
    public RestartCommandBuilder ForInstance(InstanceId instanceId)
    {
        _instanceId = instanceId;
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Sets the grace period in seconds before forcing shutdown during restart.
    /// </summary>
    public RestartCommandBuilder WithGracePeriod(int seconds)
    {
        _gracePeriod = seconds;
        return this;
    }

    /// <summary>
    /// Waits for the instance to fully restart before returning.
    /// </summary>
    public RestartCommandBuilder WithWait()
    {
        _baseBuilder.WithFlag("wait");
        return this;
    }

    /// <summary>
    /// Saves the world state before restarting.
    /// </summary>
    public RestartCommandBuilder WithSave()
    {
        _baseBuilder.WithFlag("save");
        return this;
    }

    /// <summary>
    /// Builds the final restart command.
    /// </summary>
    public Result<string> Build()
    {
        // Validate instance ID is provided
        if (_instanceId == null)
        {
            return Result.Failure<string>("Instance ID is required for restart command.");
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
