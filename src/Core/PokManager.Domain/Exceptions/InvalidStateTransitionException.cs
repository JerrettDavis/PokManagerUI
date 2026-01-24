namespace PokManager.Domain.Exceptions;

public class InvalidStateTransitionException(string from, string to) : DomainException($"Cannot transition from {from} to {to}.");
