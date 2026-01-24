using FluentAssertions;
using PokManager.Domain.Common;
using PokManager.Infrastructure.PokManager.PokManager.Parsers;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Parsers;

/// <summary>
/// Tests for ListInstancesParser using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class ListInstancesParserTests
{
    private readonly ListInstancesParser _parser = new();
    private Result<IReadOnlyList<string>> _result = null!;

    [Fact]
    public void Given_SingleInstance_When_Parse_Then_ReturnsSingleInstance()
    {
        // Given
        var output = "Instance_Server1";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(1);
        _result.Value[0].Should().Be("Server1");
    }

    [Fact]
    public void Given_MultipleInstances_When_Parse_Then_ReturnsAllInstances()
    {
        // Given
        var output = @"Instance_Server1
Instance_Server2
Instance_Server3";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value[0].Should().Be("Server1");
        _result.Value[1].Should().Be("Server2");
        _result.Value[2].Should().Be("Server3");
    }

    [Fact]
    public void Given_InstancesWithUnderscores_When_Parse_Then_PreservesNames()
    {
        // Given
        var output = @"Instance_my_test_server
Instance_another_server_01";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(2);
        _result.Value[0].Should().Be("my_test_server");
        _result.Value[1].Should().Be("another_server_01");
    }

    [Fact]
    public void Given_InstancesWithHyphens_When_Parse_Then_PreservesNames()
    {
        // Given
        var output = @"Instance_my-test-server
Instance_another-server-01";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(2);
        _result.Value[0].Should().Be("my-test-server");
        _result.Value[1].Should().Be("another-server-01");
    }

    [Fact]
    public void Given_OutputWithBlankLines_When_Parse_Then_SkipsBlankLines()
    {
        // Given
        var output = @"Instance_Server1

Instance_Server2

Instance_Server3";

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
        var output = @"  Instance_Server1
  Instance_Server2
  Instance_Server3  ";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value[0].Should().Be("Server1");
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
    public void Given_OnlyWhitespace_When_Parse_Then_ReturnsEmptyList()
    {
        // Given
        var output = @"


";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_InvalidInstanceNames_When_Parse_Then_SkipsInvalidNames()
    {
        // Given
        var output = @"Instance_Server1
NotAnInstance
Instance_Server2
SomeOtherText
Instance_Server3";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value.Should().Contain("Server1");
        _result.Value.Should().Contain("Server2");
        _result.Value.Should().Contain("Server3");
    }

    [Fact]
    public void Given_MixedCaseInstancePrefix_When_Parse_Then_ParsesCaseInsensitive()
    {
        // Given
        var output = @"instance_Server1
INSTANCE_Server2
Instance_Server3";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
    }

    [Fact]
    public void Given_DuplicateInstances_When_Parse_Then_ReturnsAllOccurrences()
    {
        // Given
        var output = @"Instance_Server1
Instance_Server2
Instance_Server1";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value.Where(x => x == "Server1").Should().HaveCount(2);
    }

    [Fact]
    public void Given_InstanceWithNumericName_When_Parse_Then_ParsesCorrectly()
    {
        // Given
        var output = @"Instance_123
Instance_456
Instance_789";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value[0].Should().Be("123");
        _result.Value[1].Should().Be("456");
        _result.Value[2].Should().Be("789");
    }

    [Fact]
    public void Given_InstanceWithSpecialCharacters_When_Parse_Then_PreservesCharacters()
    {
        // Given
        var output = @"Instance_server.prod
Instance_server-dev
Instance_server_test";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value[0].Should().Be("server.prod");
        _result.Value[1].Should().Be("server-dev");
        _result.Value[2].Should().Be("server_test");
    }

    [Fact]
    public void Given_CommaDelimitedInstances_When_Parse_Then_ParsesAll()
    {
        // Given
        var output = "Instance_Server1, Instance_Server2, Instance_Server3";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
    }

    [Fact]
    public void Given_ErrorMessage_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "Error: Failed to list instances";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("Failed to list instances");
    }

    [Fact]
    public void Given_LongInstanceNames_When_Parse_Then_PreservesFullNames()
    {
        // Given
        var output = @"Instance_production_server_east_us_region_1
Instance_staging_server_west_eu_region_2
Instance_development_server_local";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(3);
        _result.Value[0].Should().Be("production_server_east_us_region_1");
        _result.Value[1].Should().Be("staging_server_west_eu_region_2");
        _result.Value[2].Should().Be("development_server_local");
    }

    [Fact]
    public void Given_InstancesWithoutPrefix_When_Parse_Then_SkipsThem()
    {
        // Given
        var output = @"Server1
Server2
Instance_Server3";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Should().HaveCount(1);
        _result.Value[0].Should().Be("Server3");
    }
}
