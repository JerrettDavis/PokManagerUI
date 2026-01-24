using FluentAssertions;
using PokManager.Domain.ValueObjects;
using PokManager.Infrastructure.PokManager.Commands;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Commands;

public class UpdateCommandBuilderTests
{
    private const string DefaultScriptPath = "/usr/local/bin/pok.sh";

    [Fact]
    public void Build_WithInstanceId_ShouldCreateUpdateCommand()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = UpdateCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh update island_main");
    }

    [Fact]
    public void Build_WithoutInstanceId_ShouldCreateGlobalUpdateCommand()
    {
        var result = UpdateCommandBuilder
            .Create(DefaultScriptPath)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh update");
    }

    [Fact]
    public void Build_WithVersion_ShouldIncludeVersion()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = UpdateCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .ToVersion("1.2.3")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh update island_main --version 1.2.3");
    }

    [Fact]
    public void Build_WithBackupBeforeUpdate_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = UpdateCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithBackupBeforeUpdate()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh update island_main --backup-before-update");
    }

    [Fact]
    public void Build_WithValidateFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = UpdateCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithValidate()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh update island_main --validate");
    }

    [Fact]
    public void Build_WithRestartAfterUpdate_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = UpdateCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithRestartAfterUpdate()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh update island_main --restart-after-update");
    }

    [Fact]
    public void Build_WithForceFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = UpdateCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithForce()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh update island_main --force");
    }

    [Fact]
    public void Build_WithMultipleOptions_ShouldIncludeAll()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = UpdateCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .ToVersion("1.2.3")
            .WithBackupBeforeUpdate()
            .WithValidate()
            .WithRestartAfterUpdate()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh update island_main --version 1.2.3 --backup-before-update --validate --restart-after-update");
    }
}
