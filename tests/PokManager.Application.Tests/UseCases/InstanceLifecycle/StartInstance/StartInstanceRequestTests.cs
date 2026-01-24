using FluentAssertions;
using PokManager.Application.UseCases.InstanceLifecycle.StartInstance;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceLifecycle.StartInstance;

public class StartInstanceRequestTests
{
    private readonly StartInstanceRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Should_Pass_Validation()
    {
        var request = new StartInstanceRequest("island_main", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_InstanceId_Should_Fail_Validation()
    {
        var request = new StartInstanceRequest("", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Theory]
    [InlineData("island main")]
    [InlineData("island!main")]
    [InlineData("island@main")]
    public void Invalid_InstanceId_Characters_Should_Fail(string instanceId)
    {
        var request = new StartInstanceRequest(instanceId, Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InstanceId_Too_Long_Should_Fail_Validation()
    {
        var longInstanceId = new string('a', 65);
        var request = new StartInstanceRequest(longInstanceId, Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Empty_CorrelationId_Should_Fail_Validation()
    {
        var request = new StartInstanceRequest("island_main", "");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CorrelationId));
    }

    [Theory]
    [InlineData("test-instance")]
    [InlineData("test_instance")]
    [InlineData("test123")]
    [InlineData("TEST-INSTANCE-123")]
    public void Valid_InstanceId_Formats_Should_Pass(string instanceId)
    {
        var request = new StartInstanceRequest(instanceId, Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }
}
