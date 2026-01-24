namespace PokManager.Application.Models;

/// <summary>
/// Represents the severity level of a log entry.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Trace-level logging for detailed diagnostic information.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Debug-level logging for debugging information.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Information-level logging for informational messages.
    /// </summary>
    Information = 2,

    /// <summary>
    /// Warning-level logging for warning messages.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Error-level logging for error messages.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Critical-level logging for critical error messages.
    /// </summary>
    Critical = 5
}
