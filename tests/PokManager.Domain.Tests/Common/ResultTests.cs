using FluentAssertions;
using PokManager.Domain.Common;
using Xunit;

namespace PokManager.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_Result_Should_Have_Value()
    {
        var result = Result<int>.Success(42);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_Result_Should_Have_Error()
    {
        var result = Result<int>.Failure("Error message");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Error message");
    }

    [Fact]
    public void Accessing_Value_On_Failure_Should_Throw()
    {
        var result = Result<int>.Failure("Error");
        Action act = () => { _ = result.Value; };
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Accessing_Error_On_Success_Should_Throw()
    {
        var result = Result<int>.Success(42);
        Action act = () => { _ = result.Error; };
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Unit_Result_Success_Should_Work()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Generic_Failure_Helper_Should_Work()
    {
        var result = Result.Failure<int>("Error");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Error");
    }
}
