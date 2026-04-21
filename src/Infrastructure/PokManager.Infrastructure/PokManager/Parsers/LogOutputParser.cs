using System.Text.RegularExpressions;
using PokManager.Application.Models;
using PokManager.Domain.Common;

namespace PokManager.Infrastructure.PokManager.Parsers;

/// <summary>
/// Parses POK Manager log output into LogEntry objects.
/// </summary>
public class LogOutputParser
{
    private static readonly Regex s_logLinePattern = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[Z\+\-\d:]*)\s*\[(?<level>[^\]]+)\]\s*(?<message>.+)$",
        RegexOptions.Compiled
    );

    private static readonly Regex s_sourcePattern = new(
        @"\[(?<source>[^\]]+)\]",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Parses log output into a list of log entries.
    /// </summary>
    public Result<IReadOnlyList<LogEntry>> ParseLogs(string output, string instanceId)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result.Failure<IReadOnlyList<LogEntry>>("Output is null or empty");
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return Result.Failure<IReadOnlyList<LogEntry>>("Instance ID cannot be null or empty");
        }

        var logEntries = new List<LogEntry>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            var match = s_logLinePattern.Match(trimmedLine);
            if (!match.Success)
            {
                // If line doesn't match expected format, treat it as continuation of previous log
                // or as a simple info log
                if (logEntries.Count > 0)
                {
                    // Append to last log entry's message
                    var lastEntry = logEntries[^1];
                    var updatedMessage = lastEntry.Message + "\n" + trimmedLine;
                    logEntries[^1] = lastEntry with { Message = updatedMessage };
                }
                continue;
            }

            var timestampStr = match.Groups["timestamp"].Value;
            var levelStr = match.Groups["level"].Value.Trim().ToUpperInvariant();
            var message = match.Groups["message"].Value.Trim();

            // Parse timestamp
            if (!DateTimeOffset.TryParse(timestampStr, out var timestamp))
            {
                timestamp = DateTimeOffset.UtcNow;
            }

            // Parse log level
            var level = levelStr switch
            {
                "ERROR" or "ERR" => LogLevel.Error,
                "WARNING" or "WARN" or "WRN" => LogLevel.Warning,
                "INFO" or "INFORMATION" => LogLevel.Information,
                "DEBUG" or "DBG" => LogLevel.Debug,
                "TRACE" or "TRC" => LogLevel.Trace,
                _ => LogLevel.Information
            };

            // Try to extract source from message
            string? source = null;
            var sourceMatch = s_sourcePattern.Match(message);
            if (sourceMatch.Success)
            {
                source = sourceMatch.Groups["source"].Value;
            }

            var logEntry = new LogEntry(
                Timestamp: timestamp,
                Level: level,
                Message: message,
                Source: source,
                InstanceId: instanceId,
                StackTrace: null,
                AdditionalData: null
            );

            logEntries.Add(logEntry);
        }

        if (logEntries.Count == 0)
        {
            return Result.Failure<IReadOnlyList<LogEntry>>("No log entries found in output");
        }

        return Result<IReadOnlyList<LogEntry>>.Success(logEntries);
    }

    /// <summary>
    /// Parses a single log line (for streaming).
    /// </summary>
    public Result<LogEntry> ParseLogLine(string line, string instanceId)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return Result.Failure<LogEntry>("Line is null or empty");
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return Result.Failure<LogEntry>("Instance ID cannot be null or empty");
        }

        var match = s_logLinePattern.Match(line.Trim());
        if (!match.Success)
        {
            // Treat as simple info log if pattern doesn't match
            var simpleEntry = new LogEntry(
                Timestamp: DateTimeOffset.UtcNow,
                Level: LogLevel.Information,
                Message: line.Trim(),
                Source: null,
                InstanceId: instanceId,
                StackTrace: null,
                AdditionalData: null
            );
            return Result<LogEntry>.Success(simpleEntry);
        }

        var timestampStr = match.Groups["timestamp"].Value;
        var levelStr = match.Groups["level"].Value.Trim().ToUpperInvariant();
        var message = match.Groups["message"].Value.Trim();

        if (!DateTimeOffset.TryParse(timestampStr, out var timestamp))
        {
            timestamp = DateTimeOffset.UtcNow;
        }

        var level = levelStr switch
        {
            "ERROR" or "ERR" => LogLevel.Error,
            "WARNING" or "WARN" or "WRN" => LogLevel.Warning,
            "INFO" or "INFORMATION" => LogLevel.Information,
            "DEBUG" or "DBG" => LogLevel.Debug,
            "TRACE" or "TRC" => LogLevel.Trace,
            _ => LogLevel.Information
        };

        string? source = null;
        var sourceMatch = s_sourcePattern.Match(message);
        if (sourceMatch.Success)
        {
            source = sourceMatch.Groups["source"].Value;
        }

        var logEntry = new LogEntry(
            Timestamp: timestamp,
            Level: level,
            Message: message,
            Source: source,
            InstanceId: instanceId,
            StackTrace: null,
            AdditionalData: null
        );

        return Result<LogEntry>.Success(logEntry);
    }
}
