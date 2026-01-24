using FluentAssertions;
using PokManager.Domain.ValueObjects;
using PokManager.Infrastructure.PokManager.Commands;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Commands;

public class RestartCommandBuilderTests
{
    private const string DefaultScriptPath = "/usr/local/bin/pok.sh";

    [Fact]
    public void Build_WithInstanceId_ShouldCreateRestartCommand()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = RestartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh restart island_main");
    }

    [Fact]
    public void Build_WithoutInstanceId_ShouldReturnFailure()
    {
        var result = RestartCommandBuilder
            .Create(DefaultScriptPath)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Instance");
    }

    [Fact]
    public void Build_WithGracePeriod_ShouldIncludeGracePeriod()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = RestartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithGracePeriod(300)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh restart island_main --grace-period 300");
    }

    [Fact]
    public void Build_WithWaitFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = RestartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithWait()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh restart island_main --wait");
    }

    [Fact]
    public void Build_WithSaveFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = RestartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithSave()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh restart island_main --save");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Build_WithInvalidGracePeriod_ShouldReturnFailure(int invalidPeriod)
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = RestartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithGracePeriod(invalidPeriod)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Grace period");
    }

    [Fact]
    public void Build_WithMultipleOptions_ShouldIncludeAll()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = RestartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithGracePeriod(300)
            .WithWait()
            .WithSave()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh restart island_main --grace-period 300 --wait --save");
    }
}
