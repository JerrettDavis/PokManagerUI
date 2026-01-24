using FluentAssertions;
using PokManager.Domain.ValueObjects;
using PokManager.Infrastructure.PokManager.Commands;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Commands;

public class StopCommandBuilderTests
{
    private const string DefaultScriptPath = "/usr/local/bin/pok.sh";

    [Fact]
    public void Build_WithInstanceId_ShouldCreateStopCommand()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StopCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh stop island_main");
    }

    [Fact]
    public void Build_WithoutInstanceId_ShouldReturnFailure()
    {
        var result = StopCommandBuilder
            .Create(DefaultScriptPath)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Instance");
    }

    [Fact]
    public void Build_WithGracePeriod_ShouldIncludeGracePeriod()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StopCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithGracePeriod(300)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh stop island_main --grace-period 300");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Build_WithInvalidGracePeriod_ShouldReturnFailure(int invalidPeriod)
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StopCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithGracePeriod(invalidPeriod)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Grace period");
    }

    [Fact]
    public void Build_WithForceFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StopCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithForce()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh stop island_main --force");
    }

    [Fact]
    public void Build_WithSaveFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StopCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithSave()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh stop island_main --save");
    }

    [Fact]
    public void Build_WithMultipleOptions_ShouldIncludeAll()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StopCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithGracePeriod(300)
            .WithSave()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh stop island_main --grace-period 300 --save");
    }
}
