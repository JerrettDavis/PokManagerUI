namespace PokManager.Application.Models;

/// <summary>
/// Represents a single log entry from a Palworld server instance.
/// </summary>
/// <param name="Timestamp">When the log entry was created.</param>
/// <param name="Level">The severity level of the log entry.</param>
/// <param name="Message">The log message.</param>
/// <param name="Source">The source component that generated the log.</param>
/// <param name="InstanceId">The instance this log entry belongs to.</param>
/// <param name="StackTrace">Optional stack trace for error logs.</param>
/// <param name="AdditionalData">Optional additional data associated with the log entry.</param>
public record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Message,
    string? Source,
    string InstanceId,
    string? StackTrace,
    IReadOnlyDictionary<string, string>? AdditionalData
);
