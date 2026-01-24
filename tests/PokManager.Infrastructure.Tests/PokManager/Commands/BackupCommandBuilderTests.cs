using FluentAssertions;
using PokManager.Domain.ValueObjects;
using PokManager.Infrastructure.PokManager.Commands;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Commands;

public class BackupCommandBuilderTests
{
    private const string DefaultScriptPath = "/usr/local/bin/pok.sh";

    [Fact]
    public void Build_WithInstanceId_ShouldCreateBackupCommand()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = BackupCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh backup island_main");
    }

    [Fact]
    public void Build_WithoutInstanceId_ShouldReturnFailure()
    {
        var result = BackupCommandBuilder
            .Create(DefaultScriptPath)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Instance");
    }

    [Theory]
    [InlineData("gzip")]
    [InlineData("bzip2")]
    [InlineData("xz")]
    [InlineData("zstd")]
    public void Build_WithCompressionFormat_ShouldIncludeCompression(string format)
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = BackupCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithCompression(format)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be($"/usr/local/bin/pok.sh backup island_main --compress {format}");
    }

    [Fact]
    public void Build_WithOutputPath_ShouldIncludePath()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = BackupCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithOutputPath("/backups/mybackup")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("--output");
        result.Value.Should().Contain("/backups/mybackup");
    }

    [Fact]
    public void Build_WithIncrementalFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = BackupCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithIncremental()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh backup island_main --incremental");
    }

    [Fact]
    public void Build_WithExcludeLogsFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = BackupCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithExcludeLogs()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh backup island_main --exclude-logs");
    }

    [Fact]
    public void Build_WithMultipleOptions_ShouldIncludeAll()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = BackupCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithCompression("gzip")
            .WithIncremental()
            .WithExcludeLogs()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh backup island_main --compress gzip --incremental --exclude-logs");
    }

    [Fact]
    public void Build_WithDescription_ShouldIncludeDescription()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = BackupCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .WithDescription("Daily backup")
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("--description");
        result.Value.Should().Contain("'Daily backup'");
    }
}
