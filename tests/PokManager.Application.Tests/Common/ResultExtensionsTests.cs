using FluentAssertions;
using PokManager.Application.Common;
using PokManager.Domain.Common;

namespace PokManager.Application.Tests.Common;

public class ResultExtensionsTests
{
    [Fact]
    public void Bind_Should_Transform_Success_Result()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var transformed = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        transformed.IsSuccess.Should().BeTrue();
        transformed.Value.Should().Be("5");
    }

    [Fact]
    public void Bind_Should_Propagate_Failure()
    {
        // Arrange
        var result = Result<int>.Failure("Initial error");

        // Act
        var transformed = result.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        transformed.IsFailure.Should().BeTrue();
        transformed.Error.Should().Be("Initial error");
    }

    [Fact]
    public void Bind_Should_Allow_Failure_In_Transform_Function()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var transformed = result.Bind(x => Result<string>.Failure("Transform failed"));

        // Assert
        transformed.IsFailure.Should().BeTrue();
        transformed.Error.Should().Be("Transform failed");
    }

    [Fact]
    public async Task BindAsync_Should_Transform_Success_Result()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var transformed = await result.BindAsync(async x =>
        {
            await Task.Delay(1);
            return Result<string>.Success(x.ToString());
        });

        // Assert
        transformed.IsSuccess.Should().BeTrue();
        transformed.Value.Should().Be("5");
    }

    [Fact]
    public async Task BindAsync_Should_Propagate_Failure()
    {
        // Arrange
        var result = Result<int>.Failure("Initial error");

        // Act
        var transformed = await result.BindAsync(async x =>
        {
            await Task.Delay(1);
            return Result<string>.Success(x.ToString());
        });

        // Assert
        transformed.IsFailure.Should().BeTrue();
        transformed.Error.Should().Be("Initial error");
    }

    [Fact]
    public void Map_Should_Transform_Success_Result()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var transformed = result.Map(x => x.ToString());

        // Assert
        transformed.IsSuccess.Should().BeTrue();
        transformed.Value.Should().Be("5");
    }

    [Fact]
    public void Map_Should_Propagate_Failure()
    {
        // Arrange
        var result = Result<int>.Failure("Initial error");

        // Act
        var transformed = result.Map(x => x.ToString());

        // Assert
        transformed.IsFailure.Should().BeTrue();
        transformed.Error.Should().Be("Initial error");
    }

    [Fact]
    public void Tap_Should_Execute_Side_Effect_On_Success()
    {
        // Arrange
        var result = Result<int>.Success(5);
        var sideEffectExecuted = false;

        // Act
        var returned = result.Tap(x => { sideEffectExecuted = true; });

        // Assert
        sideEffectExecuted.Should().BeTrue();
        returned.IsSuccess.Should().BeTrue();
        returned.Value.Should().Be(5);
    }

    [Fact]
    public void Tap_Should_Not_Execute_Side_Effect_On_Failure()
    {
        // Arrange
        var result = Result<int>.Failure("Error");
        var sideEffectExecuted = false;

        // Act
        var returned = result.Tap(x => { sideEffectExecuted = true; });

        // Assert
        sideEffectExecuted.Should().BeFalse();
        returned.IsFailure.Should().BeTrue();
        returned.Error.Should().Be("Error");
    }

    [Fact]
    public async Task TapAsync_Should_Execute_Side_Effect_On_Success()
    {
        // Arrange
        var result = Result<int>.Success(5);
        var sideEffectExecuted = false;

        // Act
        var returned = await result.TapAsync(async x =>
        {
            await Task.Delay(1);
            sideEffectExecuted = true;
        });

        // Assert
        sideEffectExecuted.Should().BeTrue();
        returned.IsSuccess.Should().BeTrue();
        returned.Value.Should().Be(5);
    }

    [Fact]
    public async Task TapAsync_Should_Not_Execute_Side_Effect_On_Failure()
    {
        // Arrange
        var result = Result<int>.Failure("Error");
        var sideEffectExecuted = false;

        // Act
        var returned = await result.TapAsync(async x =>
        {
            await Task.Delay(1);
            sideEffectExecuted = true;
        });

        // Assert
        sideEffectExecuted.Should().BeFalse();
        returned.IsFailure.Should().BeTrue();
        returned.Error.Should().Be("Error");
    }

    [Fact]
    public void Railway_Oriented_Programming_Chain_Should_Work()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var finalResult = result
            .Map(x => x * 2)                           // 10
            .Bind(x => Result<int>.Success(x + 5))    // 15
            .Map(x => x.ToString());                   // "15"

        // Assert
        finalResult.IsSuccess.Should().BeTrue();
        finalResult.Value.Should().Be("15");
    }

    [Fact]
    public void Railway_Oriented_Programming_Chain_Should_Stop_At_First_Failure()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var finalResult = result
            .Map(x => x * 2)                                  // 10
            .Bind(x => Result<int>.Failure("Middle failed"))  // Failure
            .Map(x => x.ToString());                          // Should not execute

        // Assert
        finalResult.IsFailure.Should().BeTrue();
        finalResult.Error.Should().Be("Middle failed");
    }
}
