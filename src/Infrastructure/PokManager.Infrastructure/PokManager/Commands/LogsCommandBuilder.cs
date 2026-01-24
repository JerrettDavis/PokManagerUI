using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Builder for constructing POK Manager logs commands.
/// </summary>
public sealed class LogsCommandBuilder
{
    private readonly PokManagerCommandBuilder _baseBuilder;

    private LogsCommandBuilder(string scriptPath)
    {
        _baseBuilder = PokManagerCommandBuilder.Create(scriptPath).WithCommand("logs");
    }

    /// <summary>
    /// Creates a new logs command builder with the specified script path.
    /// </summary>
    public static LogsCommandBuilder Create(string scriptPath)
    {
        return new LogsCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the instance to get logs from.
    /// </summary>
    public LogsCommandBuilder ForInstance(InstanceId instanceId)
    {
        _baseBuilder.WithInstanceId(instanceId);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of log lines to retrieve.
    /// </summary>
    public LogsCommandBuilder WithMaxLines(int maxLines)
    {
        _baseBuilder.WithArgument("tail", maxLines.ToString());
        return this;
    }

    /// <summary>
    /// Follows the log output in real-time (streaming).
    /// </summary>
    public LogsCommandBuilder WithFollow()
    {
        _baseBuilder.WithFlag("follow");
        return this;
    }

    /// <summary>
    /// Filters logs by minimum level (e.g., "error", "warning", "info").
    /// </summary>
    public LogsCommandBuilder WithLevel(string level)
    {
        _baseBuilder.WithArgument("level", level);
        return this;
    }

    /// <summary>
    /// Filters logs since a specific timestamp.
    /// </summary>
    public LogsCommandBuilder WithSince(DateTimeOffset since)
    {
        _baseBuilder.WithArgument("since", since.ToString("o"));
        return this;
    }

    /// <summary>
    /// Filters logs until a specific timestamp.
    /// </summary>
    public LogsCommandBuilder WithUntil(DateTimeOffset until)
    {
        _baseBuilder.WithArgument("until", until.ToString("o"));
        return this;
    }

    /// <summary>
    /// Applies a text filter to log entries.
    /// </summary>
    public LogsCommandBuilder WithFilter(string filter)
    {
        _baseBuilder.WithArgument("filter", filter);
        return this;
    }

    /// <summary>
    /// Includes system-level logs in addition to application logs.
    /// </summary>
    public LogsCommandBuilder WithSystemLogs()
    {
        _baseBuilder.WithFlag("system");
        return this;
    }

    /// <summary>
    /// Requests timestamps in the output.
    /// </summary>
    public LogsCommandBuilder WithTimestamps()
    {
        _baseBuilder.WithFlag("timestamps");
        return this;
    }

    /// <summary>
    /// Builds the final logs command.
    /// </summary>
    public Result<string> Build()
    {
        return _baseBuilder.Build();
    }
}
