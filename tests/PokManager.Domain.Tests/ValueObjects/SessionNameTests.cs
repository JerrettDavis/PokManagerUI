using FluentAssertions;
using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;
using Xunit;

namespace PokManager.Domain.Tests.ValueObjects;

public class SessionNameTests
{
    [Fact]
    public void Valid_SessionName_Should_Be_Created()
    {
        var result = SessionName.Create("My Gaming Session");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("My Gaming Session");
    }

    [Theory]
    [InlineData("Island Adventure")]
    [InlineData("Session-123")]
    [InlineData("My_Server_01!")]
    [InlineData("Test@Session#1")]
    public void Valid_Patterns_Should_Succeed(string value)
    {
        var result = SessionName.Create(value);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Empty_SessionName_Should_Fail()
    {
        var result = SessionName.Create("");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void Whitespace_SessionName_Should_Fail()
    {
        var result = SessionName.Create("   ");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void SessionName_Too_Long_Should_Fail()
    {
        var result = SessionName.Create(new string('a', 129));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("128 characters");
    }

    [Fact]
    public void Equal_SessionNames_Should_Be_Equal()
    {
        var name1 = SessionName.Create("My Session").Value;
        var name2 = SessionName.Create("My Session").Value;
        name1.Should().Be(name2);
        name1.GetHashCode().Should().Be(name2.GetHashCode());
    }

    [Fact]
    public void SessionName_With_Special_Characters_Should_Succeed()
    {
        var result = SessionName.Create("Session!@#$%^&*()_+-=[]{}|;:',.<>?");
        result.IsSuccess.Should().BeTrue();
    }
}
