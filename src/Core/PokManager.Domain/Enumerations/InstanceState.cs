namespace PokManager.Domain.Enumerations;

public enum InstanceState
{
    Unknown = 0,
    Created = 1,
    Starting = 2,
    Running = 3,
    Stopping = 4,
    Stopped = 5,
    Restarting = 6,
    Failed = 7,
    Deleted = 8
}
