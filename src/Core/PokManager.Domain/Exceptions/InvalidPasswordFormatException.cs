namespace PokManager.Domain.Exceptions;

public class InvalidPasswordFormatException(string message) : DomainException(message)
{
    public InvalidPasswordFormatException()
        : this("Password format is invalid. Must be between 8 and 128 characters and contain only ASCII printable characters.")
    {
    }
}
