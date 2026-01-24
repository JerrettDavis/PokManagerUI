using FluentAssertions;
using Xunit;

namespace PokManager.Infrastructure.Tests.Fakes;

public class FakeClockTests
{
    [Fact]
    public void FakeClock_Returns_Set_Time()
    {
        var clock = new FakeClock();
        var testTime = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

        clock.SetTime(testTime);

        clock.UtcNow.Should().Be(testTime);
    }

    [Fact]
    public void FakeClock_Can_Advance_Time()
    {
        var clock = new FakeClock();
        var startTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        clock.SetTime(startTime);

        clock.Advance(TimeSpan.FromHours(2));

        clock.UtcNow.Should().Be(startTime.AddHours(2));
    }

    [Fact]
    public void FakeClock_Now_Returns_LocalTime()
    {
        var clock = new FakeClock();
        var utcTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        clock.SetTime(utcTime);

        clock.Now.Should().Be(utcTime.ToLocalTime());
    }

    [Fact]
    public void FakeClock_Reset_Sets_To_Current_Time()
    {
        var clock = new FakeClock();
        var testTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.SetTime(testTime);

        clock.Reset();

        clock.UtcNow.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void FakeClock_Can_Be_Set_To_Past_Date()
    {
        var clock = new FakeClock();
        var pastTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        clock.SetTime(pastTime);

        clock.UtcNow.Should().Be(pastTime);
    }

    [Fact]
    public void FakeClock_Can_Be_Set_To_Future_Date()
    {
        var clock = new FakeClock();
        var futureTime = new DateTimeOffset(2030, 12, 31, 23, 59, 59, TimeSpan.Zero);

        clock.SetTime(futureTime);

        clock.UtcNow.Should().Be(futureTime);
    }
}
