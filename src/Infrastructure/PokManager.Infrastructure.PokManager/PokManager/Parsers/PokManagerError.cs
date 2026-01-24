namespace PokManager.Infrastructure.PokManager.PokManager.Parsers;

/// <summary>
/// Represents a categorized error from POK Manager.
/// </summary>
/// <param name="ErrorCode">The categorized error code.</param>
/// <param name="Message">The error message.</param>
/// <param name="RawOutput">The original raw output that produced the error.</param>
public record PokManagerError(
    PokManagerErrorCode ErrorCode,
    string Message,
    string RawOutput
);

/// <summary>
/// Standard error codes for POK Manager operations.
/// </summary>
public enum PokManagerErrorCode
{
    Unknown = 0,
    InstanceNotFound = 1,
    InstanceAlreadyExists = 2,
    InstanceNotRunning = 3,
    InstanceAlreadyRunning = 4,
    PermissionDenied = 5,
    InvalidConfiguration = 6,
    InvalidState = 7,
    BackupNotFound = 8,
    InsufficientDiskSpace = 9,
    NetworkError = 10,
    TimeoutError = 11,
    ParseError = 12
}
