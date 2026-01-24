using FluentAssertions;
using PokManager.Domain.Exceptions;

namespace PokManager.Domain.Tests.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void InvalidInstanceNameException_Should_Have_Correct_Message()
    {
        // Arrange
        const string invalidName = "invalid name!";

        // Act
        var ex = new InvalidInstanceNameException(invalidName);

        // Assert
        ex.Message.Should().Contain("invalid name!");
        ex.Message.Should().Contain("alphanumeric");
        ex.Should().BeAssignableTo<DomainException>();
        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void InvalidStateTransitionException_Should_Have_Correct_Message()
    {
        // Arrange
        const string from = "Running";
        const string to = "Creating";

        // Act
        var ex = new InvalidStateTransitionException(from, to);

        // Assert
        ex.Message.Should().Contain("Running");
        ex.Message.Should().Contain("Creating");
        ex.Message.Should().Contain("Cannot transition");
        ex.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidBackupIdException_Should_Have_Correct_Message()
    {
        // Arrange
        const string invalidBackupId = "invalid-backup";

        // Act
        var ex = new InvalidBackupIdException(invalidBackupId);

        // Assert
        ex.Message.Should().Contain("invalid-backup");
        ex.Message.Should().Contain("Backup ID");
        ex.Message.Should().Contain("invalid");
        ex.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidPasswordFormatException_Should_Have_Default_Message()
    {
        // Act
        var ex = new InvalidPasswordFormatException();

        // Assert
        ex.Message.Should().Contain("Password format");
        ex.Message.Should().Contain("invalid");
        ex.Message.Should().Contain("8 and 128 characters");
        ex.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void InvalidPasswordFormatException_Should_Accept_Custom_Message()
    {
        // Arrange
        const string customMessage = "Custom password validation message";

        // Act
        var ex = new InvalidPasswordFormatException(customMessage);

        // Assert
        ex.Message.Should().Be(customMessage);
        ex.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void DomainExceptions_Should_Be_Throwable()
    {
        // Arrange & Act
        Action act = () => throw new InvalidInstanceNameException("bad_name");

        // Assert
        act.Should().Throw<InvalidInstanceNameException>()
            .Which.Message.Should().Contain("bad_name");
    }

    [Fact]
    public void DomainExceptions_Should_Be_Catchable_As_DomainException()
    {
        // Arrange
        var wasThrown = false;

        // Act
        try
        {
            throw new InvalidStateTransitionException("Start", "End");
        }
        catch (DomainException)
        {
            wasThrown = true;
        }

        // Assert
        wasThrown.Should().BeTrue();
    }

    [Fact]
    public void DomainExceptions_Should_Be_Catchable_As_Exception()
    {
        // Arrange
        var wasThrown = false;

        // Act
        try
        {
            throw new InvalidBackupIdException("test");
        }
        catch (Exception)
        {
            wasThrown = true;
        }

        // Assert
        wasThrown.Should().BeTrue();
    }
}
