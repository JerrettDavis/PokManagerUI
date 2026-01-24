using FluentAssertions;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.PokManager.PokManager.Parsers;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Parsers;

/// <summary>
/// Integration tests to validate all parsers are working correctly.
/// These tests verify the core functionality of each parser.
/// </summary>
public class ParserValidationTests
{
    [Fact]
    public void StatusOutputParser_ShouldParse_ValidOutput()
    {
        // Arrange
        var parser = new StatusOutputParser();
        var output = "Instance: TestServer, State: Running, Container: abc123, Health: Healthy";

        // Act
        var result = parser.Parse(output);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.InstanceId.Should().Be("TestServer");
        result.Value.State.Should().Be(InstanceState.Running);
        result.Value.Health.Should().Be(ProcessHealth.Healthy);
    }

    [Fact]
    public void DetailsOutputParser_ShouldParse_ValidOutput()
    {
        // Arrange
        var parser = new DetailsOutputParser();
        var output = @"SessionName: MyServer
ServerPassword: secret123
MaxPlayers: 20";

        // Act
        var result = parser.Parse(output);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey("SessionName");
        result.Value["SessionName"].Should().Be("MyServer");
        result.Value.Should().ContainKey("ServerPassword");
        result.Value["ServerPassword"].Should().Be("secret123");
        result.Value.Should().ContainKey("MaxPlayers");
        result.Value["MaxPlayers"].Should().Be("20");
    }

    [Fact]
    public void ListInstancesParser_ShouldParse_ValidOutput()
    {
        // Arrange
        var parser = new ListInstancesParser();
        var output = @"Instance_Server1
Instance_Server2
Instance_Server3";

        // Act
        var result = parser.Parse(output);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].Should().Be("Server1");
        result.Value[1].Should().Be("Server2");
        result.Value[2].Should().Be("Server3");
    }

    [Fact]
    public void BackupListParser_ShouldParse_ValidOutput()
    {
        // Arrange
        var parser = new BackupListParser();
        var output = @"backup_Server1_20250119_100000.tar.gz
backup_Server2_20250119_110000.tar.zst";

        // Act
        var result = parser.Parse(output);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].InstanceId.Should().Be("Server1");
        result.Value[0].CompressionFormat.Should().Be(CompressionFormat.Gzip);
        result.Value[1].InstanceId.Should().Be("Server2");
        result.Value[1].CompressionFormat.Should().Be(CompressionFormat.Zstd);
    }

    [Fact]
    public void ErrorOutputParser_ShouldParse_ValidOutput()
    {
        // Arrange
        var parser = new ErrorOutputParser();
        var output = "Error: Instance 'MyServer' not found";

        // Act
        var result = parser.Parse(output);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InstanceNotFound);
        result.Value.Message.Should().Contain("not found");
    }

    [Fact]
    public void AllParsers_ShouldHandleEmptyInput_Gracefully()
    {
        // This test validates that all parsers handle empty input correctly

        // StatusOutputParser should return failure for empty input
        var statusParser = new StatusOutputParser();
        statusParser.Parse("").IsFailure.Should().BeTrue();

        // DetailsOutputParser should return failure for empty input
        var detailsParser = new DetailsOutputParser();
        detailsParser.Parse("").IsFailure.Should().BeTrue();

        // ListInstancesParser should return empty list for empty input
        var listParser = new ListInstancesParser();
        listParser.Parse("").IsSuccess.Should().BeTrue();
        listParser.Parse("").Value.Should().BeEmpty();

        // BackupListParser should return empty list for empty input
        var backupParser = new BackupListParser();
        backupParser.Parse("").IsSuccess.Should().BeTrue();
        backupParser.Parse("").Value.Should().BeEmpty();

        // ErrorOutputParser should return failure for empty input
        var errorParser = new ErrorOutputParser();
        errorParser.Parse("").IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AllParsers_ShouldHandleErrorMessages_Appropriately()
    {
        var errorOutput = "Error: Something went wrong";

        // StatusOutputParser should recognize error messages
        var statusParser = new StatusOutputParser();
        statusParser.Parse(errorOutput).IsFailure.Should().BeTrue();

        // DetailsOutputParser should recognize error messages
        var detailsParser = new DetailsOutputParser();
        detailsParser.Parse(errorOutput).IsFailure.Should().BeTrue();

        // ListInstancesParser should recognize error messages
        var listParser = new ListInstancesParser();
        listParser.Parse(errorOutput).IsFailure.Should().BeTrue();

        // BackupListParser should recognize error messages
        var backupParser = new BackupListParser();
        backupParser.Parse(errorOutput).IsFailure.Should().BeTrue();

        // ErrorOutputParser should successfully parse error messages
        var errorParser = new ErrorOutputParser();
        errorParser.Parse(errorOutput).IsSuccess.Should().BeTrue();
    }
}
