using FluentAssertions;
using PokManager.Application.Models;
using PokManager.Application.UseCases.BackupManagement.CreateBackup;
using Xunit;

namespace PokManager.Application.Tests.UseCases.BackupManagement.CreateBackup;

public class CreateBackupRequestTests
{
    private readonly CreateBackupRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Without_Label_Should_Pass_Validation()
    {
        var request = new CreateBackupRequest("island_main", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Request_With_Label_Should_Pass_Validation()
    {
        var request = new CreateBackupRequest("island_main", Guid.NewGuid().ToString(),
            new CreateBackupOptions(Description: "Pre-update backup"));
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_InstanceId_Should_Fail_Validation()
    {
        var request = new CreateBackupRequest("", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Empty_CorrelationId_Should_Fail_Validation()
    {
        var request = new CreateBackupRequest("island_main", "");
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CorrelationId));
    }

    [Fact]
    public void Label_Too_Long_Should_Fail_Validation()
    {
        var longLabel = new string('a', 257);
        var request = new CreateBackupRequest("island_main", Guid.NewGuid().ToString(),
            new CreateBackupOptions(Description: longLabel));
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Description"));
    }

    [Fact]
    public void Invalid_InstanceId_Format_Should_Fail_Validation()
    {
        var request = new CreateBackupRequest("island@main", Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Theory]
    [InlineData("Before update")]
    [InlineData("Daily backup")]
    [InlineData("Safety backup - 2024-01-19")]
    public void Valid_Label_Values_Should_Pass(string label)
    {
        var request = new CreateBackupRequest("island_main", Guid.NewGuid().ToString(),
            new CreateBackupOptions(Description: label));
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Label_At_Max_Length_Should_Pass_Validation()
    {
        var maxLabel = new string('a', 256);
        var request = new CreateBackupRequest("island_main", Guid.NewGuid().ToString(),
            new CreateBackupOptions(Description: maxLabel));
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }
}
