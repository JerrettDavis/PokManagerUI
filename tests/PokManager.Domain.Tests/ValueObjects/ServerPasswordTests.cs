using FluentAssertions;
using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;
using Xunit;

namespace PokManager.Domain.Tests.ValueObjects;

public class ServerPasswordTests
{
    [Fact]
    public void Valid_ServerPassword_Should_Be_Created()
    {
        var result = ServerPassword.Create("Pass1234");
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Pass1234");
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData("Password123")]
    [InlineData("ABC123")]
    [InlineData("1234567890")]
    public void Valid_Patterns_Should_Succeed(string value)
    {
        var result = ServerPassword.Create(value);
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("pass word", "alphanumeric")]
    [InlineData("pass!word", "alphanumeric")]
    [InlineData("pass@123", "alphanumeric")]
    [InlineData("pass-123", "alphanumeric")]
    public void Invalid_Characters_Should_Fail(string value, string errorSubstring)
    {
        var result = ServerPassword.Create(value);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(errorSubstring);
    }

    [Fact]
    public void Empty_ServerPassword_Should_Fail()
    {
        var result = ServerPassword.Create("");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void ServerPassword_Too_Short_Should_Fail()
    {
        var result = ServerPassword.Create("abc");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("4 characters");
    }

    [Fact]
    public void ServerPassword_Too_Long_Should_Fail()
    {
        var result = ServerPassword.Create(new string('a', 65));
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("64 characters");
    }

    [Fact]
    public void Equal_ServerPasswords_Should_Be_Equal()
    {
        var pwd1 = ServerPassword.Create("Pass1234").Value;
        var pwd2 = ServerPassword.Create("Pass1234").Value;
        pwd1.Should().Be(pwd2);
        pwd1.GetHashCode().Should().Be(pwd2.GetHashCode());
    }
}
