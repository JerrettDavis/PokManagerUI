using FluentAssertions;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.PokManager.PokManager.Parsers;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Parsers;

/// <summary>
/// Tests for BackupListParser using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class BackupListParserTests
{
    private readonly BackupListParser _parser = new();
    private Result<IReadOnlyList<ParsedBackupInfo>> _result = null!;

    [Fact]
    public void Given_SingleBackupWithGzip_When_Parse_Then_ParsesBackupInfo()
    {
        // Given
        var output = "backup_MyServer_20250119_143022.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(1);
        _result.Value[0].InstanceId.Should().Be("MyServer");
        _result.Value[0].Timestamp.Year.Should().Be(2025);
        _result.Value[0].Timestamp.Month.Should().Be(1);
        _result.Value[0].Timestamp.Day.Should().Be(19);
        _result.Value[0].Timestamp.Hour.Should().Be(14);
        _result.Value[0].Timestamp.Minute.Should().Be(30);
        _result.Value[0].Timestamp.Second.Should().Be(22);
        _result.Value[0].CompressionFormat.Should().Be(CompressionFormat.Gzip);
    }

    [Fact]
    public void Given_SingleBackupWithZstd_When_Parse_Then_ParsesBackupInfo()
    {
        // Given
        var output = "backup_MyServer_20250119_143022.tar.zst";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(1);
        _result.Value[0].CompressionFormat.Should().Be(CompressionFormat.Zstd);
    }

    [Fact]
    public void Given_MultipleBackups_When_Parse_Then_ParsesAllBackups()
    {
        // Given
        var output = @"backup_Server1_20250119_100000.tar.gz
backup_Server2_20250119_110000.tar.gz
backup_Server3_20250119_120000.tar.zst";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value[0].InstanceId.Should().Be("Server1");
        _result.Value[1].InstanceId.Should().Be("Server2");
        _result.Value[2].InstanceId.Should().Be("Server3");
        _result.Value[2].CompressionFormat.Should().Be(CompressionFormat.Zstd);
    }

    [Fact]
    public void Given_BackupWithUnderscoresInName_When_Parse_Then_ParsesCorrectly()
    {
        // Given
        var output = "backup_my_test_server_01_20250119_143022.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value[0].InstanceId.Should().Be("my_test_server_01");
    }

    [Fact]
    public void Given_BackupWithHyphensInName_When_Parse_Then_ParsesCorrectly()
    {
        // Given
        var output = "backup_my-test-server-01_20250119_143022.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value[0].InstanceId.Should().Be("my-test-server-01");
    }

    [Fact]
    public void Given_BackupWithFullPath_When_Parse_Then_ExtractsFilename()
    {
        // Given
        var output = "/opt/palworld/backups/backup_MyServer_20250119_143022.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(1);
        _result.Value[0].InstanceId.Should().Be("MyServer");
    }

    [Fact]
    public void Given_BackupWithWindowsPath_When_Parse_Then_ExtractsFilename()
    {
        // Given
        var output = @"C:\backups\backup_MyServer_20250119_143022.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(1);
        _result.Value[0].InstanceId.Should().Be("MyServer");
    }

    [Fact]
    public void Given_OutputWithBlankLines_When_Parse_Then_SkipsBlankLines()
    {
        // Given
        var output = @"backup_Server1_20250119_100000.tar.gz

backup_Server2_20250119_110000.tar.gz

backup_Server3_20250119_120000.tar.zst";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
    }

    [Fact]
    public void Given_OutputWithExtraWhitespace_When_Parse_Then_TrimsWhitespace()
    {
        // Given
        var output = @"  backup_Server1_20250119_100000.tar.gz
  backup_Server2_20250119_110000.tar.gz  ";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(2);
    }

    [Fact]
    public void Given_EmptyOutput_When_Parse_Then_ReturnsEmptyList()
    {
        // Given
        var output = "";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_NullOutput_When_Parse_Then_ReturnsEmptyList()
    {
        // Given
        string output = null!;

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_InvalidBackupFilename_When_Parse_Then_SkipsInvalidFiles()
    {
        // Given
        var output = @"backup_Server1_20250119_100000.tar.gz
some_other_file.txt
backup_Server2_20250119_110000.tar.gz
invalid_backup.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(2);
        _result.Value[0].InstanceId.Should().Be("Server1");
        _result.Value[1].InstanceId.Should().Be("Server2");
    }

    [Fact]
    public void Given_BackupWithInvalidDate_When_Parse_Then_SkipsInvalidBackup()
    {
        // Given
        var output = @"backup_Server1_20251399_100000.tar.gz
backup_Server2_20250119_110000.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(1);
        _result.Value[0].InstanceId.Should().Be("Server2");
    }

    [Fact]
    public void Given_BackupWithInvalidTime_When_Parse_Then_SkipsInvalidBackup()
    {
        // Given
        var output = @"backup_Server1_20250119_256000.tar.gz
backup_Server2_20250119_110000.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(1);
        _result.Value[0].InstanceId.Should().Be("Server2");
    }

    [Fact]
    public void Given_BackupWithUnknownCompression_When_Parse_Then_UsesUnknownFormat()
    {
        // Given
        var output = "backup_MyServer_20250119_143022.tar.unknown";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(1);
        _result.Value[0].CompressionFormat.Should().Be(CompressionFormat.Unknown);
    }

    [Fact]
    public void Given_BackupsSortedByTime_When_Parse_Then_PreservesOrder()
    {
        // Given
        var output = @"backup_Server1_20250119_100000.tar.gz
backup_Server1_20250119_110000.tar.gz
backup_Server1_20250119_120000.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value[0].Timestamp.Hour.Should().Be(10);
        _result.Value[1].Timestamp.Hour.Should().Be(11);
        _result.Value[2].Timestamp.Hour.Should().Be(12);
    }

    [Fact]
    public void Given_ErrorMessage_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "Error: Failed to list backups";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("Failed to list backups");
    }

    [Fact]
    public void Given_MixedValidAndInvalidBackups_When_Parse_Then_ParsesOnlyValid()
    {
        // Given
        var output = @"backup_Server1_20250119_100000.tar.gz
not_a_backup.txt
backup_Server2_invalid_date.tar.gz
backup_Server3_20250119_120000.tar.zst
backup_Server4_20250119_999999.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(2);
        _result.Value[0].InstanceId.Should().Be("Server1");
        _result.Value[1].InstanceId.Should().Be("Server3");
    }

    [Fact]
    public void Given_BackupFileNameOnly_When_Parse_Then_ParsesCorrectly()
    {
        // Given
        var output = "backup_production_server_20250119_143022.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(1);
        _result.Value[0].InstanceId.Should().Be("production_server");
        _result.Value[0].FileName.Should().Be("backup_production_server_20250119_143022.tar.gz");
    }

    [Fact]
    public void Given_CommaDelimitedBackups_When_Parse_Then_ParsesAll()
    {
        // Given
        var output = "backup_Server1_20250119_100000.tar.gz, backup_Server2_20250119_110000.tar.gz";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(2);
    }
}
