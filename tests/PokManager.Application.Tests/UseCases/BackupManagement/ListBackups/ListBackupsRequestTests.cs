using FluentAssertions;
using PokManager.Application.UseCases.BackupManagement.ListBackups;
using Xunit;

namespace PokManager.Application.Tests.UseCases.BackupManagement.ListBackups;

public class ListBackupsRequestTests
{
    private readonly ListBackupsRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Without_Metadata_Should_Pass_Validation()
    {
        var request = new ListBackupsRequest(
            InstanceId: "island_main",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Request_With_Metadata_Should_Pass_Validation()
    {
        var request = new ListBackupsRequest(
            InstanceId: "island_main",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: true
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_InstanceId_Should_Fail_Validation()
    {
        var request = new ListBackupsRequest(
            InstanceId: "",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Empty_CorrelationId_Should_Fail_Validation()
    {
        var request = new ListBackupsRequest(
            InstanceId: "island_main",
            CorrelationId: "",
            IncludeMetadata: false
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CorrelationId));
    }

    [Fact]
    public void InstanceId_Too_Long_Should_Fail_Validation()
    {
        var longInstanceId = new string('a', 65);
        var request = new ListBackupsRequest(
            InstanceId: longInstanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Invalid_InstanceId_Format_Should_Fail_Validation()
    {
        var request = new ListBackupsRequest(
            InstanceId: "island@main",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Theory]
    [InlineData("island_main")]
    [InlineData("island-test-123")]
    [InlineData("ISLAND_MAIN")]
    [InlineData("island123")]
    [InlineData("i")]
    public void Valid_InstanceId_Formats_Should_Pass(string instanceId)
    {
        var request = new ListBackupsRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InstanceId_At_Max_Length_Should_Pass_Validation()
    {
        var maxInstanceId = new string('a', 64);
        var request = new ListBackupsRequest(
            InstanceId: maxInstanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("island main")]
    [InlineData("island.main")]
    [InlineData("island/main")]
    [InlineData("island\\main")]
    [InlineData("island*main")]
    public void Invalid_InstanceId_Characters_Should_Fail_Validation(string instanceId)
    {
        var request = new ListBackupsRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }
}
