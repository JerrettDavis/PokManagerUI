using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager status commands.
/// </summary>
public sealed class StatusCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;

    private StatusCommandBuilder(string scriptPath)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand("status");
    }

    /// <summary>
    /// Creates a new status command builder with the specified script path.
    /// </summary>
    public static StatusCommandBuilder Create(string scriptPath)
    {
        return new StatusCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the instance to check status for. If not called, checks status of all instances.
    /// </summary>
    public StatusCommandBuilder ForInstance(InstanceId instanceId)
    {
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Enables verbose output with detailed information.
    /// </summary>
    public StatusCommandBuilder WithVerbose()
    {
        _baseBuilder.WithFlag("verbose");
        return this;
    }

    /// <summary>
    /// Requests JSON-formatted output.
    /// </summary>
    public StatusCommandBuilder WithJsonOutput()
    {
        _baseBuilder.WithFlag("json");
        return this;
    }

    /// <summary>
    /// Builds the final status command.
    /// </summary>
    public Result<string> Build()
    {
        return _baseBuilder.Build();
    }
}
