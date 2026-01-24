using FluentAssertions;
using PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;
using Xunit;

namespace PokManager.Application.Tests.UseCases.ConfigurationManagement;

public class GetConfigurationRequestTests
{
    private readonly GetConfigurationRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Should_Pass_Validation()
    {
        var request = new GetConfigurationRequest("island_main", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Request_With_IncludeSecrets_True_Should_Pass_Validation()
    {
        var request = new GetConfigurationRequest("island_main", Guid.NewGuid().ToString(), true);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Request_With_IncludeSecrets_False_Should_Pass_Validation()
    {
        var request = new GetConfigurationRequest("island_main", Guid.NewGuid().ToString(), false);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_InstanceId_Should_Fail_Validation()
    {
        var request = new GetConfigurationRequest("", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Empty_CorrelationId_Should_Fail_Validation()
    {
        var request = new GetConfigurationRequest("island_main", "");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CorrelationId));
    }

    [Fact]
    public void InstanceId_Too_Long_Should_Fail_Validation()
    {
        var longInstanceId = new string('a', 65);
        var request = new GetConfigurationRequest(longInstanceId, Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void InstanceId_At_Max_Length_Should_Pass_Validation()
    {
        var maxInstanceId = new string('a', 64);
        var request = new GetConfigurationRequest(maxInstanceId, Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_InstanceId_Format_Should_Fail_Validation()
    {
        var request = new GetConfigurationRequest("island@main", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Theory]
    [InlineData("island_main")]
    [InlineData("server-01")]
    [InlineData("test123")]
    [InlineData("my_server-v2")]
    [InlineData("SERVER_123")]
    public void Valid_InstanceId_Formats_Should_Pass(string instanceId)
    {
        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("island@main")]
    [InlineData("server.01")]
    [InlineData("test#123")]
    [InlineData("my server")]
    [InlineData("server/01")]
    public void Invalid_InstanceId_Formats_Should_Fail(string instanceId)
    {
        var request = new GetConfigurationRequest(instanceId, Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }
}
