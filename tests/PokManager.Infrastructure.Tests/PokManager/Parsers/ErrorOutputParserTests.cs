using FluentAssertions;
using PokManager.Domain.Common;
using PokManager.Infrastructure.PokManager.PokManager.Parsers;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Parsers;

/// <summary>
/// Tests for ErrorOutputParser using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class ErrorOutputParserTests
{
    private readonly ErrorOutputParser _parser = new();
    private Result<PokManagerError> _result = null!;

    [Fact]
    public void Given_InstanceNotFoundError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Instance 'MyServer' not found";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InstanceNotFound);
        _result.Value.Message.Should().Contain("not found");
        _result.Value.RawOutput.Should().Be(output);
    }

    [Fact]
    public void Given_InstanceAlreadyExistsError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Instance 'MyServer' already exists";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InstanceAlreadyExists);
        _result.Value.Message.Should().Contain("already exists");
    }

    [Fact]
    public void Given_PermissionDeniedError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Permission denied to access instance";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.PermissionDenied);
        _result.Value.Message.Should().Contain("Permission denied");
    }

    [Fact]
    public void Given_InstanceNotRunningError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Instance is not running";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InstanceNotRunning);
        _result.Value.Message.Should().Contain("not running");
    }

    [Fact]
    public void Given_InstanceAlreadyRunningError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Instance is already running";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InstanceAlreadyRunning);
        _result.Value.Message.Should().Contain("already running");
    }

    [Fact]
    public void Given_InvalidConfigurationError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Invalid configuration provided";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InvalidConfiguration);
        _result.Value.Message.Should().Contain("Invalid configuration");
    }

    [Fact]
    public void Given_InvalidStateError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Cannot perform operation in current state";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InvalidState);
        _result.Value.Message.Should().Contain("current state");
    }

    [Fact]
    public void Given_BackupNotFoundError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Backup file not found";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.BackupNotFound);
        _result.Value.Message.Should().Contain("Backup");
    }

    [Fact]
    public void Given_InsufficientDiskSpaceError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Insufficient disk space for operation";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InsufficientDiskSpace);
        _result.Value.Message.Should().Contain("disk space");
    }

    [Fact]
    public void Given_NetworkError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Network connection failed";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.NetworkError);
        _result.Value.Message.Should().Contain("Network");
    }

    [Fact]
    public void Given_TimeoutError_When_Parse_Then_CategorizeCorrectly()
    {
        // Given
        var output = "Error: Operation timed out after 30 seconds";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.TimeoutError);
        _result.Value.Message.Should().Contain("timed out");
    }

    [Fact]
    public void Given_UnknownError_When_Parse_Then_CategorizeAsUnknown()
    {
        // Given
        var output = "Error: Something completely unexpected happened";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.Unknown);
        _result.Value.Message.Should().Contain("Something completely unexpected");
    }

    [Fact]
    public void Given_ErrorWithoutErrorPrefix_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "This is just a regular message";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("not an error message");
    }

    [Fact]
    public void Given_EmptyString_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("empty");
    }

    [Fact]
    public void Given_NullString_When_Parse_Then_ReturnsFailure()
    {
        // Given
        string output = null!;

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("null or empty");
    }

    [Fact]
    public void Given_ErrorWithExtraWhitespace_When_Parse_Then_TrimsWhitespace()
    {
        // Given
        var output = "  Error:   Instance not found   ";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InstanceNotFound);
        _result.Value.Message.Should().Be("Instance not found");
    }

    [Fact]
    public void Given_CaseInsensitiveErrorPrefix_When_Parse_Then_ParsesCorrectly()
    {
        // Given
        var output = "error: Instance not found";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InstanceNotFound);
    }

    [Fact]
    public void Given_MultilineError_When_Parse_Then_PreservesFullMessage()
    {
        // Given
        var output = @"Error: Permission denied to access instance
Additional details: User lacks required privileges
Contact administrator for access";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.PermissionDenied);
        _result.Value.Message.Should().Contain("Permission denied");
        _result.Value.RawOutput.Should().Contain("Additional details");
    }

    [Fact]
    public void Given_ErrorWithSpecialCharacters_When_Parse_Then_PreservesMessage()
    {
        // Given
        var output = "Error: Instance 'server-01_prod' not found in path C:\\servers";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InstanceNotFound);
        _result.Value.Message.Should().Contain("server-01_prod");
    }

    [Fact]
    public void Given_MultipleErrorKeywords_When_Parse_Then_UsesFirstMatch()
    {
        // Given
        var output = "Error: Instance not found, but backup also not found";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        // Should match InstanceNotFound first as it appears first
        _result.Value.ErrorCode.Should().Be(PokManagerErrorCode.InstanceNotFound);
    }
}
