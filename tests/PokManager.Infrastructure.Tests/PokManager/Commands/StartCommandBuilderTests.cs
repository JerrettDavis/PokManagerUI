using FluentAssertions;
using PokManager.Domain.ValueObjects;
using PokManager.Infrastructure.PokManager.Commands;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Commands;

public class StartCommandBuilderTests
{
    private const string DefaultScriptPath = "/usr/local/bin/pok.sh";

    [Fact]
    public void Build_WithInstanceId_ShouldCreateStartCommand()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh start island_main");
    }

    [Fact]
    public void Build_WithoutInstanceId_ShouldReturnFailure()
    {
        var result = StartCommandBuilder
            .Create(DefaultScriptPath)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Instance");
    }

    [Fact]
    public void Build_WithEmptyScriptPath_ShouldReturnFailure()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StartCommandBuilder
            .Create("")
            .ForInstance(instanceId)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Script path");
    }

    [Fact]
    public void Build_WithDetachedFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithDetached()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh start island_main --detached");
    }

    [Fact]
    public void Build_WithWaitFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithWait()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh start island_main --wait");
    }

    [Fact]
    public void Build_WithTimeout_ShouldIncludeTimeout()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithTimeout(300)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh start island_main --timeout 300");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Build_WithInvalidTimeout_ShouldReturnFailure(int invalidTimeout)
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithTimeout(invalidTimeout)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Timeout");
    }

    [Fact]
    public void Build_WithMultipleOptions_ShouldIncludeAll()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = StartCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithDetached()
            .WithTimeout(300)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh start island_main --timeout 300 --detached");
    }
}
