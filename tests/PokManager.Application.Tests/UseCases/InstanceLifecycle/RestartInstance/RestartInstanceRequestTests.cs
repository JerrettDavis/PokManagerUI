using FluentAssertions;
using PokManager.Application.UseCases.InstanceLifecycle.RestartInstance;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceLifecycle.RestartInstance;

public class RestartInstanceRequestTests
{
    private readonly RestartInstanceRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_With_Defaults_Should_Pass_Validation()
    {
        var request = new RestartInstanceRequest("island_main", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Request_With_Custom_Values_Should_Pass_Validation()
    {
        var request = new RestartInstanceRequest("island_main", Guid.NewGuid().ToString(), GracePeriodSeconds: 60, SaveWorld: true, WaitForHealthy: false);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_InstanceId_Should_Fail_Validation()
    {
        var request = new RestartInstanceRequest("", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Empty_CorrelationId_Should_Fail_Validation()
    {
        var request = new RestartInstanceRequest("island_main", "");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CorrelationId));
    }

    [Fact]
    public void Negative_GracePeriodSeconds_Should_Fail_Validation()
    {
        var request = new RestartInstanceRequest("island_main", Guid.NewGuid().ToString(), GracePeriodSeconds: -1);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.GracePeriodSeconds));
    }

    [Fact]
    public void GracePeriodSeconds_Over_300_Should_Fail_Validation()
    {
        var request = new RestartInstanceRequest("island_main", Guid.NewGuid().ToString(), GracePeriodSeconds: 301);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.GracePeriodSeconds));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(300)]
    public void Valid_GracePeriodSeconds_Should_Pass(int gracePeriodSeconds)
    {
        var request = new RestartInstanceRequest("island_main", Guid.NewGuid().ToString(), GracePeriodSeconds: gracePeriodSeconds);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_InstanceId_Format_Should_Fail_Validation()
    {
        var request = new RestartInstanceRequest("island main", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }
}
