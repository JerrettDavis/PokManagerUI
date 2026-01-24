using FluentAssertions;
using PokManager.Domain.Enumerations;

namespace PokManager.Domain.Tests.Enumerations;

public class InstanceStateTests
{
    [Fact]
    public void InstanceState_Should_Have_Correct_Values()
    {
        InstanceState.Unknown.Should().Be((InstanceState)0);
        InstanceState.Created.Should().Be((InstanceState)1);
        InstanceState.Starting.Should().Be((InstanceState)2);
        InstanceState.Running.Should().Be((InstanceState)3);
        InstanceState.Stopping.Should().Be((InstanceState)4);
        InstanceState.Stopped.Should().Be((InstanceState)5);
        InstanceState.Restarting.Should().Be((InstanceState)6);
        InstanceState.Failed.Should().Be((InstanceState)7);
        InstanceState.Deleted.Should().Be((InstanceState)8);
    }

    [Fact]
    public void InstanceState_Should_Have_All_Expected_Members()
    {
        var values = Enum.GetValues<InstanceState>();
        values.Should().Contain(InstanceState.Unknown);
        values.Should().Contain(InstanceState.Created);
        values.Should().Contain(InstanceState.Starting);
        values.Should().Contain(InstanceState.Running);
        values.Should().Contain(InstanceState.Stopping);
        values.Should().Contain(InstanceState.Stopped);
        values.Should().Contain(InstanceState.Restarting);
        values.Should().Contain(InstanceState.Failed);
        values.Should().Contain(InstanceState.Deleted);
    }
}
