using FluentAssertions;
using PokManager.Domain.ValueObjects;
using PokManager.Infrastructure.PokManager.Commands;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Commands;

public class PokManagerCommandBuilderTests
{
    private const string DefaultScriptPath = "/usr/local/bin/pok.sh";

    [Fact]
    public void Build_WithValidCommand_ShouldReturnSuccess()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = PokManagerCommandBuilder
            .Create(DefaultScriptPath)
            .WithCommand("status")
            .WithInstanceId(instanceId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh status island_main");
    }

    [Fact]
    public void Build_WithoutCommand_ShouldReturnFailure()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = PokManagerCommandBuilder
            .Create(DefaultScriptPath)
            .WithInstanceId(instanceId)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Command is required");
    }

    [Fact]
    public void Build_WithEmptyScriptPath_ShouldReturnFailure()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = PokManagerCommandBuilder
            .Create("")
            .WithCommand("status")
            .WithInstanceId(instanceId)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Script path");
    }

    [Fact]
    public void Build_WithFlagArgument_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = PokManagerCommandBuilder
            .Create(DefaultScriptPath)
            .WithCommand("stop")
            .WithInstanceId(instanceId)
            .WithFlag("force")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh stop island_main --force");
    }

    [Fact]
    public void Build_WithKeyValueArgument_ShouldIncludeKeyValue()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = PokManagerCommandBuilder
            .Create(DefaultScriptPath)
            .WithCommand("create")
            .WithInstanceId(instanceId)
            .WithArgument("map", "TheIsland")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh create island_main --map TheIsland");
    }

    [Fact]
    public void Build_WithMultipleArguments_ShouldIncludeAll()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = PokManagerCommandBuilder
            .Create(DefaultScriptPath)
            .WithCommand("create")
            .WithInstanceId(instanceId)
            .WithArgument("map", "TheIsland")
            .WithArgument("players", "20")
            .WithFlag("force")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh create island_main --map TheIsland --players 20 --force");
    }

    [Fact]
    public void Build_WithSpecialCharactersInValue_ShouldEscapeValue()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = PokManagerCommandBuilder
            .Create(DefaultScriptPath)
            .WithCommand("create")
            .WithInstanceId(instanceId)
            .WithArgument("name", "My Server's World")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("'My Server'\\''s World'");
    }

    [Fact]
    public void Build_WithSpacesInScriptPath_ShouldEscapePath()
    {
        var instanceId = InstanceId.Create("island_main").Value;
        var scriptPath = "/usr/local/pok manager/pok.sh";

        var result = PokManagerCommandBuilder
            .Create(scriptPath)
            .WithCommand("status")
            .WithInstanceId(instanceId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("'/usr/local/pok manager/pok.sh'");
    }

    [Fact]
    public void Build_WithoutInstanceId_ShouldSucceedForGlobalCommands()
    {
        var result = PokManagerCommandBuilder
            .Create(DefaultScriptPath)
            .WithCommand("list")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh list");
    }

    [Fact]
    public void Build_WithDangerousCharactersInArgument_ShouldReturnFailure()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = PokManagerCommandBuilder
            .Create(DefaultScriptPath)
            .WithCommand("create")
            .WithInstanceId(instanceId)
            .WithArgument("name", "test;rm -rf /")
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("dangerous characters");
    }

    [Fact]
    public void Build_WithPathTraversal_ShouldReturnFailure()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = PokManagerCommandBuilder
            .Create(DefaultScriptPath)
            .WithCommand("backup")
            .WithInstanceId(instanceId)
            .WithArgument("path", "../../../etc/passwd")
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("path traversal");
    }

    [Theory]
    [InlineData("test&echo")]
    [InlineData("test|cat")]
    [InlineData("test;whoami")]
    [InlineData("test`whoami`")]
    [InlineData("test$(whoami)")]
    public void Build_WithCommandInjectionAttempts_ShouldReturnFailure(string maliciousValue)
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = PokManagerCommandBuilder
            .Create(DefaultScriptPath)
            .WithCommand("create")
            .WithInstanceId(instanceId)
            .WithArgument("name", maliciousValue)
            .Build();

        result.IsFailure.Should().BeTrue();
    }
}
