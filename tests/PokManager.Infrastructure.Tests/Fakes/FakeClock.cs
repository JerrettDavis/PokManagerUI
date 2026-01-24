using PokManager.Application.Ports;

namespace PokManager.Infrastructure.Tests.Fakes;

/// <summary>
/// Fake clock implementation with controllable time for testing.
/// </summary>
public class FakeClock : IClock
{
    private DateTimeOffset _currentTime = DateTimeOffset.UtcNow;

    public DateTimeOffset Now => _currentTime.ToLocalTime();
    public DateTimeOffset UtcNow => _currentTime;

    /// <summary>
    /// Set the current time to a specific value.
    /// </summary>
    public void SetTime(DateTimeOffset time)
    {
        _currentTime = time;
    }

    /// <summary>
    /// Advance time by a specific duration.
    /// </summary>
    public void Advance(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }

    /// <summary>
    /// Reset to current system time.
    /// </summary>
    public void Reset()
    {
        _currentTime = DateTimeOffset.UtcNow;
    }
}
