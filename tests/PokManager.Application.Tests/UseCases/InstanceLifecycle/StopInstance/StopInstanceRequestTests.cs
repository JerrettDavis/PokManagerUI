using FluentAssertions;
using PokManager.Application.UseCases.InstanceLifecycle.StopInstance;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceLifecycle.StopInstance;

public class StopInstanceRequestTests
{
    private readonly StopInstanceRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_With_Defaults_Should_Pass_Validation()
    {
        var request = new StopInstanceRequest("island_main", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Request_With_Custom_Values_Should_Pass_Validation()
    {
        var request = new StopInstanceRequest("island_main", Guid.NewGuid().ToString(), ForceKill: true, TimeoutSeconds: 60);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_InstanceId_Should_Fail_Validation()
    {
        var request = new StopInstanceRequest("", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Empty_CorrelationId_Should_Fail_Validation()
    {
        var request = new StopInstanceRequest("island_main", "");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CorrelationId));
    }

    [Fact]
    public void Zero_TimeoutSeconds_Should_Fail_Validation()
    {
        var request = new StopInstanceRequest("island_main", Guid.NewGuid().ToString(), TimeoutSeconds: 0);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.TimeoutSeconds));
    }

    [Fact]
    public void Negative_TimeoutSeconds_Should_Fail_Validation()
    {
        var request = new StopInstanceRequest("island_main", Guid.NewGuid().ToString(), TimeoutSeconds: -1);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.TimeoutSeconds));
    }

    [Fact]
    public void TimeoutSeconds_Over_300_Should_Fail_Validation()
    {
        var request = new StopInstanceRequest("island_main", Guid.NewGuid().ToString(), TimeoutSeconds: 301);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.TimeoutSeconds));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(300)]
    public void Valid_TimeoutSeconds_Should_Pass(int timeoutSeconds)
    {
        var request = new StopInstanceRequest("island_main", Guid.NewGuid().ToString(), TimeoutSeconds: timeoutSeconds);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_InstanceId_Format_Should_Fail_Validation()
    {
        var request = new StopInstanceRequest("island main", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }
}
