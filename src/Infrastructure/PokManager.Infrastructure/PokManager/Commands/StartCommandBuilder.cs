using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager start commands.
/// </summary>
public sealed class StartCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;
    private InstanceId? _instanceId;
    private int? _timeout;

    private StartCommandBuilder(string scriptPath)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand("start");
    }

    /// <summary>
    /// Creates a new start command builder with the specified script path.
    /// </summary>
    public static StartCommandBuilder Create(string scriptPath)
    {
        return new StartCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the instance to start. Required for start command.
    /// </summary>
    public StartCommandBuilder ForInstance(InstanceId instanceId)
    {
        _instanceId = instanceId;
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Starts the instance in detached mode (runs in background).
    /// </summary>
    public StartCommandBuilder WithDetached()
    {
        _baseBuilder.WithFlag("detached");
        return this;
    }

    /// <summary>
    /// Waits for the instance to fully start before returning.
    /// </summary>
    public StartCommandBuilder WithWait()
    {
        _baseBuilder.WithFlag("wait");
        return this;
    }

    /// <summary>
    /// Sets the timeout in seconds for the start operation.
    /// </summary>
    public StartCommandBuilder WithTimeout(int seconds)
    {
        _timeout = seconds;
        return this;
    }

    /// <summary>
    /// Builds the final start command.
    /// </summary>
    public Result<string> Build()
    {
        // Validate instance ID is provided
        if (_instanceId == null)
        {
            return Result.Failure<string>("Instance ID is required for start command.");
        }

        // Validate timeout if provided
        if (_timeout.HasValue && _timeout.Value <= 0)
        {
            return Result.Failure<string>("Timeout must be a positive number.");
        }

        // Add timeout if specified
        if (_timeout.HasValue)
        {
            _baseBuilder.WithArgument("timeout", _timeout.Value.ToString());
        }

        return _baseBuilder.Build();
    }
}
