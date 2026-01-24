using FluentAssertions;
using PokManager.Domain.Entities;
using PokManager.Domain.Enumerations;
using Xunit;

namespace PokManager.Domain.Tests.Entities;

public class InstanceTests
{
    [Fact]
    public void Instance_Can_Be_Created()
    {
        var instance = new Instance(
            "island_main",
            "My Island Server",
            "TheIsland_WP",
            50);

        instance.SessionName.Should().Be("My Island Server");
        instance.State.Should().Be(InstanceState.Created);
    }

    [Fact]
    public void Instance_Can_Transition_From_Stopped_To_Starting()
    {
        var instance = new Instance("island_main", "Server", "Map", 50);
        instance.State = InstanceState.Stopped;

        var result = instance.TransitionTo(InstanceState.Starting);

        result.IsSuccess.Should().BeTrue();
        instance.State.Should().Be(InstanceState.Starting);
    }

    [Fact]
    public void Instance_Cannot_Transition_From_Stopped_To_Running_Directly()
    {
        var instance = new Instance("island_main", "Server", "Map", 50);
        instance.State = InstanceState.Stopped;

        var result = instance.TransitionTo(InstanceState.Running);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid state transition");
        instance.State.Should().Be(InstanceState.Stopped);
    }

    [Fact]
    public void Instance_Can_Transition_From_Starting_To_Running()
    {
        var instance = new Instance("island_main", "Server", "Map", 50);
        instance.State = InstanceState.Starting;

        var result = instance.TransitionTo(InstanceState.Running);

        result.IsSuccess.Should().BeTrue();
        instance.State.Should().Be(InstanceState.Running);
    }

    [Theory]
    [InlineData(InstanceState.Running, InstanceState.Stopping, true)]
    [InlineData(InstanceState.Running, InstanceState.Restarting, true)]
    [InlineData(InstanceState.Stopped, InstanceState.Deleted, true)]
    [InlineData(InstanceState.Created, InstanceState.Starting, true)]
    [InlineData(InstanceState.Stopped, InstanceState.Running, false)]
    [InlineData(InstanceState.Created, InstanceState.Running, false)]
    public void State_Transition_Rules_Should_Be_Enforced(
        InstanceState from,
        InstanceState to,
        bool shouldSucceed)
    {
        var instance = new Instance("island_main", "Server", "Map", 50);
        instance.State = from;

        var result = instance.TransitionTo(to);

        result.IsSuccess.Should().Be(shouldSucceed);
        if (shouldSucceed)
            instance.State.Should().Be(to);
        else
            instance.State.Should().Be(from);
    }
}
