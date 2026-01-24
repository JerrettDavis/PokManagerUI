namespace PokManager.Domain.Enumerations;

public enum OperationType
{
    Unknown = 0,
    StartInstance = 1,
    StopInstance = 2,
    RestartInstance = 3,
    CreateBackup = 4,
    RestoreBackup = 5,
    DeleteBackup = 6,
    ApplyConfiguration = 7,
    CheckUpdates = 8,
    ApplyUpdates = 9,
    CreateInstance = 10,
    DeleteInstance = 11,
    SaveWorld = 12,
    SendChatMessage = 13,
    ExecuteCustomCommand = 14
}
