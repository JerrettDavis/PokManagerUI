using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager configuration commands.
/// </summary>
public sealed class ConfigCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;

    private ConfigCommandBuilder(string scriptPath, string subCommand)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand($"config {subCommand}");
    }

    /// <summary>
    /// Creates a new configuration get command builder.
    /// </summary>
    public static ConfigCommandBuilder CreateGet(string scriptPath)
    {
        return new ConfigCommandBuilder(scriptPath, "get");
    }

    /// <summary>
    /// Creates a new configuration validate command builder.
    /// </summary>
    public static ConfigCommandBuilder CreateValidate(string scriptPath)
    {
        return new ConfigCommandBuilder(scriptPath, "validate");
    }

    /// <summary>
    /// Creates a new configuration apply command builder.
    /// </summary>
    public static ConfigCommandBuilder CreateApply(string scriptPath)
    {
        return new ConfigCommandBuilder(scriptPath, "apply");
    }

    /// <summary>
    /// Sets the instance for the configuration command.
    /// </summary>
    public ConfigCommandBuilder ForInstance(InstanceId instanceId)
    {
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Adds a configuration key-value pair to set.
    /// </summary>
    public ConfigCommandBuilder WithSetting(string key, string value)
    {
        _baseBuilder.WithArgument(key, value);
        return this;
    }

    /// <summary>
    /// Creates a backup before applying configuration changes.
    /// </summary>
    public ConfigCommandBuilder WithBackup()
    {
        _baseBuilder.WithFlag("backup");
        return this;
    }

    /// <summary>
    /// Validates the configuration before applying.
    /// </summary>
    public ConfigCommandBuilder WithValidate()
    {
        _baseBuilder.WithFlag("validate");
        return this;
    }

    /// <summary>
    /// Restarts the instance after applying configuration if needed.
    /// </summary>
    public ConfigCommandBuilder WithRestartIfNeeded()
    {
        _baseBuilder.WithFlag("restart-if-needed");
        return this;
    }

    /// <summary>
    /// Specifies a configuration file to read from or write to.
    /// </summary>
    public ConfigCommandBuilder WithConfigFile(string configFilePath)
    {
        _baseBuilder.WithArgument("config-file", configFilePath);
        return this;
    }

    /// <summary>
    /// Builds the final configuration command.
    /// </summary>
    public Result<string> Build()
    {
        return _baseBuilder.Build();
    }
}
