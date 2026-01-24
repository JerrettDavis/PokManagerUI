namespace PokManager.Application.Models;

/// <summary>
/// Options for retrieving logs from a Palworld server instance.
/// </summary>
/// <param name="MaxLines">Maximum number of log lines to retrieve.</param>
/// <param name="Since">Only retrieve logs since this time.</param>
/// <param name="Until">Only retrieve logs until this time.</param>
/// <param name="MinLevel">Minimum log level to include.</param>
/// <param name="Filter">Optional text filter to apply to log entries.</param>
/// <param name="IncludeSystemLogs">Whether to include system-level logs in addition to application logs.</param>
public record GetLogsOptions(
    int MaxLines = 1000,
    DateTimeOffset? Since = null,
    DateTimeOffset? Until = null,
    LogLevel MinLevel = LogLevel.Information,
    string? Filter = null,
    bool IncludeSystemLogs = false
);
