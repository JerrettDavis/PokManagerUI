using FluentAssertions;
using PokManager.Application.UseCases.BackupManagement.RestoreBackup;
using Xunit;

namespace PokManager.Application.Tests.UseCases.BackupManagement.RestoreBackup;

public class RestoreBackupRequestTests
{
    private readonly RestoreBackupRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_With_Confirmed_True_Should_Pass_Validation()
    {
        var request = new RestoreBackupRequest(
            "island_main",
            "backup_123",
            Guid.NewGuid().ToString(),
            Confirmed: true);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Request_With_Confirmed_False_Should_Fail_Validation()
    {
        var request = new RestoreBackupRequest(
            "island_main",
            "backup_123",
            Guid.NewGuid().ToString(),
            Confirmed: false);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Confirmed");
    }

    [Fact]
    public void Empty_InstanceId_Should_Fail_Validation()
    {
        var request = new RestoreBackupRequest(
            "",
            "backup_123",
            Guid.NewGuid().ToString(),
            Confirmed: true);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Empty_BackupId_Should_Fail_Validation()
    {
        var request = new RestoreBackupRequest(
            "island_main",
            "",
            Guid.NewGuid().ToString(),
            Confirmed: true);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.BackupId));
    }

    [Fact]
    public void Empty_CorrelationId_Should_Fail_Validation()
    {
        var request = new RestoreBackupRequest(
            "island_main",
            "backup_123",
            "",
            Confirmed: true);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.CorrelationId));
    }

    [Fact]
    public void Invalid_InstanceId_Format_Should_Fail_Validation()
    {
        var request = new RestoreBackupRequest(
            "island main",
            "backup_123",
            Guid.NewGuid().ToString(),
            Confirmed: true);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.InstanceId));
    }

    [Fact]
    public void Valid_Request_With_CreateSafetyBackup_False_Should_Pass()
    {
        var request = new RestoreBackupRequest(
            "island_main",
            "backup_123",
            Guid.NewGuid().ToString(),
            Confirmed: true,
            CreateSafetyBackup: false);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Default_Request_Without_Confirmed_Should_Fail_Validation()
    {
        var request = new RestoreBackupRequest(
            "island_main",
            "backup_123",
            Guid.NewGuid().ToString());
        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Confirmed");
    }

    [Theory]
    [InlineData("backup-123")]
    [InlineData("backup_456")]
    [InlineData("20240119-backup")]
    public void Valid_BackupId_Formats_Should_Pass(string backupId)
    {
        var request = new RestoreBackupRequest(
            "island_main",
            backupId,
            Guid.NewGuid().ToString(),
            Confirmed: true);
        var result = _validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }
}
