using FluentAssertions;
using PokManager.Domain.ValueObjects;
using PokManager.Infrastructure.PokManager.Commands;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Commands;

public class StatusCommandBuilderTests
{
    private const string DefaultScriptPath = "/usr/local/bin/pok.sh";

    [Fact]
    public void Build_WithInstanceId_ShouldCreateStatusCommand()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StatusCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh status island_main");
    }

    [Fact]
    public void Build_WithoutInstanceId_ShouldCreateGlobalStatusCommand()
    {
        var result = StatusCommandBuilder
            .Create(DefaultScriptPath)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh status");
    }

    [Fact]
    public void Build_WithEmptyScriptPath_ShouldReturnFailure()
    {
        var result = StatusCommandBuilder
            .Create("")
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Script path");
    }

    [Fact]
    public void Build_WithVerboseFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StatusCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithVerbose()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh status island_main --verbose");
    }

    [Fact]
    public void Build_WithJsonFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StatusCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithJsonOutput()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh status island_main --json");
    }

    [Fact]
    public void Build_WithMultipleFlags_ShouldIncludeAll()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StatusCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithVerbose()
            .WithJsonOutput()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh status island_main --verbose --json");
    }
}
