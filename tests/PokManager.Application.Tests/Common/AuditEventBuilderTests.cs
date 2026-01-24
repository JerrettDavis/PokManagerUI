using FluentAssertions;
using PokManager.Application.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Application.Tests.Common;

public class AuditEventBuilderTests
{
    [Fact]
    public void Build_Should_Create_Valid_AuditEvent()
    {
        // Arrange
        var builder = new AuditEventBuilder();

        // Act
        var auditEvent = builder
            .WithOperationType("BackupCreated")
            .WithCorrelationId("test-correlation-id")
            .WithInstanceId("instance-123")
            .WithOutcome(OperationOutcome.Completed)
            .Build();

        // Assert
        auditEvent.Should().NotBeNull();
        auditEvent.OperationType.Should().Be("BackupCreated");
        auditEvent.CorrelationId.Should().Be("test-correlation-id");
        auditEvent.InstanceId.Should().Be("instance-123");
        auditEvent.Outcome.Should().Be(OperationOutcome.Completed);
    }

    [Fact]
    public void Build_Without_OperationType_Should_Throw()
    {
        // Arrange
        var builder = new AuditEventBuilder()
            .WithCorrelationId("test-correlation-id");

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OperationType is required*");
    }

    [Fact]
    public void Build_Without_CorrelationId_Should_Throw()
    {
        // Arrange
        var builder = new AuditEventBuilder()
            .WithOperationType("BackupCreated");

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CorrelationId is required*");
    }

    [Fact]
    public void WithError_Should_Set_Failed_Outcome()
    {
        // Arrange
        var builder = new AuditEventBuilder();

        // Act
        var auditEvent = builder
            .WithOperationType("BackupCreated")
            .WithCorrelationId("test-correlation-id")
            .WithError("Something went wrong")
            .Build();

        // Assert
        auditEvent.Outcome.Should().Be(OperationOutcome.Failed);
        auditEvent.ErrorMessage.Should().Be("Something went wrong");
    }

    [Fact]
    public void WithMetadata_Should_Add_Metadata_Entry()
    {
        // Arrange
        var builder = new AuditEventBuilder();

        // Act
        var auditEvent = builder
            .WithOperationType("BackupCreated")
            .WithCorrelationId("test-correlation-id")
            .WithMetadata("fileName", "backup.bak")
            .WithMetadata("fileSize", 1024)
            .Build();

        // Assert
        auditEvent.Metadata.Should().ContainKey("fileName");
        auditEvent.Metadata["fileName"].Should().Be("backup.bak");
        auditEvent.Metadata.Should().ContainKey("fileSize");
        auditEvent.Metadata["fileSize"].Should().Be(1024);
    }

    [Fact]
    public void Build_Should_Set_Timestamp()
    {
        // Arrange
        var builder = new AuditEventBuilder();
        var beforeBuild = DateTimeOffset.UtcNow;

        // Act
        var auditEvent = builder
            .WithOperationType("BackupCreated")
            .WithCorrelationId("test-correlation-id")
            .Build();

        var afterBuild = DateTimeOffset.UtcNow;

        // Assert
        auditEvent.Timestamp.Should().BeOnOrAfter(beforeBuild);
        auditEvent.Timestamp.Should().BeOnOrBefore(afterBuild);
    }

    [Fact]
    public void InstanceId_Should_Be_Optional()
    {
        // Arrange
        var builder = new AuditEventBuilder();

        // Act
        var auditEvent = builder
            .WithOperationType("BackupCreated")
            .WithCorrelationId("test-correlation-id")
            .Build();

        // Assert
        auditEvent.InstanceId.Should().BeNull();
    }

    [Fact]
    public void Default_Outcome_Should_Be_Pending()
    {
        // Arrange
        var builder = new AuditEventBuilder();

        // Act
        var auditEvent = builder
            .WithOperationType("BackupCreated")
            .WithCorrelationId("test-correlation-id")
            .Build();

        // Assert
        auditEvent.Outcome.Should().Be(OperationOutcome.Pending);
    }
}
