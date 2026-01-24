using PokManager.Application.Ports;

namespace PokManager.Infrastructure.Common;

/// <summary>
/// Production implementation of IClock that returns actual system time.
/// </summary>
public class SystemClock : IClock
{
    /// <summary>
    /// Gets the current date and time in the local timezone.
    /// </summary>
    public DateTimeOffset Now => DateTimeOffset.Now;

    /// <summary>
    /// Gets the current date and time in UTC.
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
