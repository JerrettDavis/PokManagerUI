using FluentAssertions;
using PokManager.Application.Common;

namespace PokManager.Application.Tests.Common;

public class ConfirmationGateTests
{
    [Fact]
    public void Confirmed_Operation_Should_Succeed()
    {
        // Act
        var result = ConfirmationGate.RequireConfirmation(true, "RestoreBackup");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Unconfirmed_Operation_Should_Fail()
    {
        // Act
        var result = ConfirmationGate.RequireConfirmation(false, "RestoreBackup");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("requires explicit confirmation");
    }

    [Fact]
    public void Unconfirmed_Operation_Should_Include_Operation_Name_In_Error()
    {
        // Act
        var result = ConfirmationGate.RequireConfirmation(false, "DeleteAllData");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("DeleteAllData");
    }

    [Fact]
    public void Unconfirmed_Operation_Should_Include_Confirmation_Instructions()
    {
        // Act
        var result = ConfirmationGate.RequireConfirmation(false, "RestoreBackup");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Set Confirmed = true to proceed");
    }
}
