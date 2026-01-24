using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager RCON commands.
/// </summary>
public sealed class RconCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;

    private RconCommandBuilder(string scriptPath)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand("rcon");
    }

    /// <summary>
    /// Creates a new RCON command builder with the specified script path.
    /// </summary>
    public static RconCommandBuilder Create(string scriptPath)
    {
        return new RconCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the instance to execute RCON command on.
    /// </summary>
    public RconCommandBuilder ForInstance(InstanceId instanceId)
    {
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Sets the RCON command to execute.
    /// </summary>
    public RconCommandBuilder WithCommand(string command)
    {
        _baseBuilder.WithArgument("command", command);
        return this;
    }

    /// <summary>
    /// Sends a broadcast message to all players.
    /// </summary>
    public RconCommandBuilder WithBroadcast(string message)
    {
        _baseBuilder.WithArgument("broadcast", message);
        return this;
    }

    /// <summary>
    /// Triggers a world save operation.
    /// </summary>
    public RconCommandBuilder WithSave()
    {
        _baseBuilder.WithFlag("save");
        return this;
    }

    /// <summary>
    /// Requests information about players.
    /// </summary>
    public RconCommandBuilder WithShowPlayers()
    {
        _baseBuilder.WithFlag("showplayers");
        return this;
    }

    /// <summary>
    /// Requests server information.
    /// </summary>
    public RconCommandBuilder WithInfo()
    {
        _baseBuilder.WithFlag("info");
        return this;
    }

    /// <summary>
    /// Builds the final RCON command.
    /// </summary>
    public Result<string> Build()
    {
        return _baseBuilder.Build();
    }
}
