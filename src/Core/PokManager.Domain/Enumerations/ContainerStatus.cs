namespace PokManager.Domain.Enumerations;

/// <summary>
/// Represents the status of a Docker container for an instance.
/// This is separate from InstanceState, which represents the logical state.
/// </summary>
public enum ContainerStatus
{
    /// <summary>Container status could not be determined</summary>
    Unknown = 0,

    /// <summary>Container exists and is running</summary>
    Running = 1,

    /// <summary>Container exists but is stopped/exited</summary>
    Stopped = 2,

    /// <summary>No container exists for this instance (data-only)</summary>
    NotCreated = 3,

    /// <summary>docker-compose file is missing or invalid</summary>
    MissingComposeFile = 4,

    /// <summary>Container exists but is in restarting state</summary>
    Restarting = 5,

    /// <summary>Container exists but is paused</summary>
    Paused = 6,

    /// <summary>Container is in dead/failed state</summary>
    Dead = 7
}
