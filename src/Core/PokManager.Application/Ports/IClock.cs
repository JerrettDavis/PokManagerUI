namespace PokManager.Application.Ports;

/// <summary>
/// Interface for obtaining current time information.
/// This abstraction enables deterministic testing of time-dependent behavior.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current date and time in the local timezone.
    /// </summary>
    DateTimeOffset Now { get; }

    /// <summary>
    /// Gets the current date and time in UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
