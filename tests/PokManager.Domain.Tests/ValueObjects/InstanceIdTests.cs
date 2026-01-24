using FluentAssertions;
using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;
using Xunit;

namespace PokManager.Domain.Tests.ValueObjects;

public class InstanceIdTests
{
    [Fact]
    public void Valid_InstanceId_Should_Be_Created()
    {
        var result = InstanceId.Create("island_main");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("island_main");
    }

    [Theory]
    [InlineData("island-main")]
    [InlineData("Island123")]
    [InlineData("my_server_01")]
    public void Valid_Patterns_Should_Succeed(string value)
    {
        var result = InstanceId.Create(value);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("island main", "alphanumeric")]
    [InlineData("island!main", "alphanumeric")]
    [InlineData("island@main", "alphanumeric")]
    public void Invalid_Characters_Should_Fail(string value, string errorSubstring)
    {
        var result = InstanceId.Create(value);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(errorSubstring);
    }

    [Fact]
    public void Empty_InstanceId_Should_Fail()
    {
        var result = InstanceId.Create("");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void InstanceId_Too_Long_Should_Fail()
    {
        var result = InstanceId.Create(new string('a', 65));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("64 characters");
    }

    [Fact]
    public void Equal_InstanceIds_Should_Be_Equal()
    {
        var id1 = InstanceId.Create("island_main").Value;
        var id2 = InstanceId.Create("island_main").Value;
        id1.Should().Be(id2);
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }
}
