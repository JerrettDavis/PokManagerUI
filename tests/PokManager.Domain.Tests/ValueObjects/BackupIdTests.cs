using FluentAssertions;
using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;
using Xunit;

namespace PokManager.Domain.Tests.ValueObjects;

public class BackupIdTests
{
    [Fact]
    public void Valid_BackupId_Should_Be_Created()
    {
        var result = BackupId.Create("island_main_backup_2025-01-19_14-30-00");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("island_main_backup_2025-01-19_14-30-00");
    }

    [Fact]
    public void BackupId_From_Components_Should_Be_Created()
    {
        var instanceId = InstanceId.Create("island_main").Value;
        var timestamp = new DateTime(2025, 1, 19, 14, 30, 0);

        var result = BackupId.CreateFromComponents(instanceId, timestamp);
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("island_main_backup_2025-01-19_14-30-00");
    }

    [Fact]
    public void BackupId_Should_Parse_InstanceId()
    {
        var result = BackupId.Create("island_main_backup_2025-01-19_14-30-00");
        result.IsSuccess.Should().BeTrue();
        result.Value.InstanceId.Should().Be("island_main");
    }

    [Fact]
    public void BackupId_Should_Parse_Timestamp()
    {
        var result = BackupId.Create("island_main_backup_2025-01-19_14-30-00");
        result.IsSuccess.Should().BeTrue();
        result.Value.Timestamp.Should().Be(new DateTime(2025, 1, 19, 14, 30, 0));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("island_main_2025-01-19")]
    [InlineData("island_main_backup_invalid")]
    [InlineData("_backup_2025-01-19_14-30-00")]
    public void Invalid_Format_Should_Fail(string value)
    {
        var result = BackupId.Create(value);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("format");
    }

    [Fact]
    public void Empty_BackupId_Should_Fail()
    {
        var result = BackupId.Create("");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void Equal_BackupIds_Should_Be_Equal()
    {
        var id1 = BackupId.Create("island_main_backup_2025-01-19_14-30-00").Value;
        var id2 = BackupId.Create("island_main_backup_2025-01-19_14-30-00").Value;
        id1.Should().Be(id2);
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void BackupId_With_Hyphens_In_InstanceId_Should_Work()
    {
        var result = BackupId.Create("island-main_backup_2025-01-19_14-30-00");
        result.IsSuccess.Should().BeTrue();
        result.Value.InstanceId.Should().Be("island-main");
    }
}
