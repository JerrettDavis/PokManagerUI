namespace PokManager.Domain.Exceptions;

public class InvalidInstanceNameException(string instanceName)
    : DomainException($"Instance name '{instanceName}' is invalid. Must contain only alphanumeric characters, hyphens, and underscores.");
