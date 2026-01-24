using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager create instance commands.
/// </summary>
public sealed class CreateInstanceCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;
    private InstanceId? _instanceId;
    private int? _maxPlayers;
    private int? _port;

    private CreateInstanceCommandBuilder(string scriptPath)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand("create");
    }

    /// <summary>
    /// Creates a new create instance command builder with the specified script path.
    /// </summary>
    public static CreateInstanceCommandBuilder Create(string scriptPath)
    {
        return new CreateInstanceCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the instance to create. Required for create command.
    /// </summary>
    public CreateInstanceCommandBuilder ForInstance(InstanceId instanceId)
    {
        _instanceId = instanceId;
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Sets the map/world to use (e.g., TheIsland, ScorchedEarth).
    /// </summary>
    public CreateInstanceCommandBuilder WithMap(string mapName)
    {
        _baseBuilder.WithArgument("map", mapName);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of players allowed on the server.
    /// </summary>
    public CreateInstanceCommandBuilder WithMaxPlayers(int maxPlayers)
    {
        _maxPlayers = maxPlayers;
        return this;
    }

    /// <summary>
    /// Sets the server port number.
    /// </summary>
    public CreateInstanceCommandBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }

    /// <summary>
    /// Sets the server name that will be displayed in the server browser.
    /// </summary>
    public CreateInstanceCommandBuilder WithServerName(string serverName)
    {
        _baseBuilder.WithArgument("server-name", serverName);
        return this;
    }

    /// <summary>
    /// Sets the server password for access control.
    /// </summary>
    public CreateInstanceCommandBuilder WithPassword(ServerPassword password)
    {
        _baseBuilder.WithArgument("password", password.Value);
        return this;
    }

    /// <summary>
    /// Makes the server publicly visible in the server browser.
    /// </summary>
    public CreateInstanceCommandBuilder WithPublic()
    {
        _baseBuilder.WithFlag("public");
        return this;
    }

    /// <summary>
    /// Sets the server to PvE (Player vs Environment) mode.
    /// </summary>
    public CreateInstanceCommandBuilder WithPvE()
    {
        _baseBuilder.WithFlag("pve");
        return this;
    }

    /// <summary>
    /// Starts the instance immediately after creation.
    /// </summary>
    public CreateInstanceCommandBuilder WithStartAfterCreate()
    {
        _baseBuilder.WithFlag("start-after-create");
        return this;
    }

    /// <summary>
    /// Builds the final create instance command.
    /// </summary>
    public Result<string> Build()
    {
        // Validate instance ID is provided
        if (_instanceId == null)
        {
            return Result.Failure<string>("Instance ID is required for create command.");
        }

        // Validate max players if provided
        if (_maxPlayers.HasValue && _maxPlayers.Value <= 0)
        {
            return Result.Failure<string>("Max players must be a positive number.");
        }

        // Validate port if provided
        if (_port.HasValue && (_port.Value < 1024 || _port.Value > 65535))
        {
            return Result.Failure<string>("Port must be between 1024 and 65535.");
        }

        // Add max players if specified
        if (_maxPlayers.HasValue)
        {
            _baseBuilder.WithArgument("players", _maxPlayers.Value.ToString());
        }

        // Add port if specified
        if (_port.HasValue)
        {
            _baseBuilder.WithArgument("port", _port.Value.ToString());
        }

        return _baseBuilder.Build();
    }
}
