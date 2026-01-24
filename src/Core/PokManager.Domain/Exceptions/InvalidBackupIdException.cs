namespace PokManager.Domain.Exceptions;

public class InvalidBackupIdException(string backupId)
    : DomainException($"Backup ID '{backupId}' is invalid. Must be a valid GUID or timestamp-based identifier.");
