using FluentAssertions;
using PokManager.Application.UseCases.Configuration.ApplyConfiguration;
using Xunit;

namespace PokManager.Application.Tests.UseCases.Configuration.ApplyConfiguration;

public class ApplyConfigurationRequestTests
{
    private readonly ApplyConfigurationRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_With_Single_Setting_Should_Pass_Validation()
    {
        var settings = new Dictionary<string, string>
        {
            ["DifficultyOffset"] = "1.0"
        };
        var request = new ApplyConfigurationRequest(
            "island_main",
            Guid.NewGuid().ToString(),
            settings);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Request_With_Multiple_Settings_Should_Pass_Validation()
    {
        var settings = new Dictionary<string, string>
        {
            ["DifficultyOffset"] = "1.0",
            ["XPMultiplier"] = "2.0",
            ["TamingSpeedMultiplier"] = "3.0"
        };
        var request = new ApplyConfigurationRequest(
            "island_main",
            Guid.NewGuid().ToString(),
            settings);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Request_With_RestartInstance_True_Should_Pass()
    {
        var settings = new Dictionary<string, string>
        {
            ["DifficultyOffset"] = "1.0"
        };
        var request = new ApplyConfigurationRequest(
            "island_main",
            Guid.NewGuid().ToString(),
            settings,
            RestartInstance: true);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_InstanceId_Should_Fail_Validation()
    {
        var settings = new Dictionary<string, string>
        {
            ["DifficultyOffset"] = "1.0"
        };
        var request = new ApplyConfigurationRequest(
            "",
            Guid.NewGuid().ToString(),
            settings);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Empty_CorrelationId_Should_Fail_Validation()
    {
        var settings = new Dictionary<string, string>
        {
            ["DifficultyOffset"] = "1.0"
        };
        var request = new ApplyConfigurationRequest(
            "island_main",
            "",
            settings);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CorrelationId));
    }

    [Fact]
    public void Empty_ConfigurationSettings_Should_Fail_Validation()
    {
        var settings = new Dictionary<string, string>();
        var request = new ApplyConfigurationRequest(
            "island_main",
            Guid.NewGuid().ToString(),
            settings);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ConfigurationSettings));
    }

    [Fact]
    public void Invalid_InstanceId_Format_Should_Fail_Validation()
    {
        var settings = new Dictionary<string, string>
        {
            ["DifficultyOffset"] = "1.0"
        };
        var request = new ApplyConfigurationRequest(
            "island main",
            Guid.NewGuid().ToString(),
            settings);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void ConfigurationSettings_With_Empty_Key_Should_Fail_Validation()
    {
        var settings = new Dictionary<string, string>
        {
            [""] = "1.0"
        };
        var request = new ApplyConfigurationRequest(
            "island_main",
            Guid.NewGuid().ToString(),
            settings);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConfigurationSettings_With_Various_Game_Settings_Should_Pass()
    {
        var settings = new Dictionary<string, string>
        {
            ["DifficultyOffset"] = "1.0",
            ["XPMultiplier"] = "2.0",
            ["TamingSpeedMultiplier"] = "3.0",
            ["HarvestAmountMultiplier"] = "2.5",
            ["PlayerDamageMultiplier"] = "1.5"
        };
        var request = new ApplyConfigurationRequest(
            "island_main",
            Guid.NewGuid().ToString(),
            settings);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }
}
