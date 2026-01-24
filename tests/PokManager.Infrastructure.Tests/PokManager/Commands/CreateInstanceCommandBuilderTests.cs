using FluentAssertions;
using PokManager.Domain.ValueObjects;
using PokManager.Infrastructure.PokManager.Commands;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Commands;

public class CreateInstanceCommandBuilderTests
{
    private const string DefaultScriptPath = "/usr/local/bin/pok.sh";

    [Fact]
    public void Build_WithInstanceId_ShouldCreateCreateCommand()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh create island_main");
    }

    [Fact]
    public void Build_WithoutInstanceId_ShouldReturnFailure()
    {
        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Instance");
    }

    [Theory]
    [InlineData("TheIsland")]
    [InlineData("ScorchedEarth")]
    [InlineData("Aberration")]
    public void Build_WithMap_ShouldIncludeMap(string mapName)
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithMap(mapName)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be($"/usr/local/bin/pok.sh create island_main --map {mapName}");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(32)]
    public void Build_WithMaxPlayers_ShouldIncludeMaxPlayers(int maxPlayers)
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithMaxPlayers(maxPlayers)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be($"/usr/local/bin/pok.sh create island_main --players {maxPlayers}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Build_WithInvalidMaxPlayers_ShouldReturnFailure(int invalidPlayers)
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithMaxPlayers(invalidPlayers)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Max players");
    }

    [Theory]
    [InlineData(7777)]
    [InlineData(8211)]
    public void Build_WithPort_ShouldIncludePort(int port)
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithPort(port)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be($"/usr/local/bin/pok.sh create island_main --port {port}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(70000)]
    public void Build_WithInvalidPort_ShouldReturnFailure(int invalidPort)
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithPort(invalidPort)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Port");
    }

    [Fact]
    public void Build_WithServerName_ShouldIncludeServerName()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithServerName("My Awesome Server")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("--server-name");
        result.Value.Should().Contain("'My Awesome Server'");
    }

    [Fact]
    public void Build_WithPassword_ShouldIncludePassword()
    {
        var instanceId = InstanceId.Create("island_main").Value;
        var password = ServerPassword.Create("MySecurePass123").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithPassword(password)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("--password");
        result.Value.Should().Contain("MySecurePass123");
    }

    [Fact]
    public void Build_WithPublicFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithPublic()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh create island_main --public");
    }

    [Fact]
    public void Build_WithPvEFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithPvE()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh create island_main --pve");
    }

    [Fact]
    public void Build_WithStartAfterCreate_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithStartAfterCreate()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh create island_main --start-after-create");
    }

    [Fact]
    public void Build_WithAllOptions_ShouldIncludeAll()
    {
        var instanceId = InstanceId.Create("island_main").Value;
        var password = ServerPassword.Create("MySecurePass123").Value;

        var result = CreateInstanceCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithMap("TheIsland")
            .WithMaxPlayers(20)
            .WithPort(7777)
            .WithServerName("My Server")
            .WithPassword(password)
            .WithPublic()
            .WithPvE()
            .WithStartAfterCreate()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("island_main");
        result.Value.Should().Contain("--map TheIsland");
        result.Value.Should().Contain("--players 20");
        result.Value.Should().Contain("--port 7777");
        result.Value.Should().Contain("--server-name");
        result.Value.Should().Contain("--password");
        result.Value.Should().Contain("--public");
        result.Value.Should().Contain("--pve");
        result.Value.Should().Contain("--start-after-create");
    }
}
