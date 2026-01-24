using FluentAssertions;
using PokManager.Domain.Enumerations;

namespace PokManager.Domain.Tests.Enumerations;

public class ProcessHealthTests
{
    [Fact]
    public void ProcessHealth_Should_Have_Correct_Values()
    {
        ProcessHealth.Unknown.Should().Be((ProcessHealth)0);
        ProcessHealth.Healthy.Should().Be((ProcessHealth)1);
        ProcessHealth.Degraded.Should().Be((ProcessHealth)2);
        ProcessHealth.Unhealthy.Should().Be((ProcessHealth)3);
    }

    [Fact]
    public void ProcessHealth_Should_Have_All_Expected_Members()
    {
        var values = Enum.GetValues<ProcessHealth>();
        values.Should().Contain(ProcessHealth.Unknown);
        values.Should().Contain(ProcessHealth.Healthy);
        values.Should().Contain(ProcessHealth.Degraded);
        values.Should().Contain(ProcessHealth.Unhealthy);
    }
}
