using PokManager.Domain.Enumerations;

namespace PokManager.Application.Models;

/// <summary>
/// Represents detailed information about a Palworld server instance.
/// </summary>
/// <param name="InstanceId">The unique identifier of the instance.</param>
/// <param name="ServerName">The display name of the server.</param>
/// <param name="State">The current state of the instance.</param>
/// <param name="Health">The health status of the instance process.</param>
/// <param name="Port">The port number the server is listening on.</param>
/// <param name="MaxPlayers">Maximum number of players allowed.</param>
/// <param name="PlayerCount">Current number of connected players.</param>
/// <param name="Version">The current server version.</param>
/// <param name="Uptime">How long the instance has been running.</param>
/// <param name="InstallPath">The file system path where the server is installed.</param>
/// <param name="WorldPath">The file system path to the world save data.</param>
/// <param name="ConfigPath">The file system path to the server configuration.</param>
/// <param name="CreatedAt">When the instance was created.</param>
/// <param name="LastStartedAt">When the instance was last started.</param>
/// <param name="LastStoppedAt">When the instance was last stopped.</param>
/// <param name="Configuration">The current server configuration settings.</param>
/// <param name="ContainerStatus">The status of the Docker container (e.g., Running, Stopped, NotCreated).</param>
/// <param name="ContainerId">The Docker container ID, if a container exists.</param>
/// <param name="DockerComposeFilePath">The path to the docker-compose file, if it exists.</param>
/// <param name="HasValidDockerCompose">Indicates whether a valid docker-compose file exists for this instance.</param>
public record InstanceDetails(
    string InstanceId,
    string ServerName,
    InstanceState State,
    ProcessHealth Health,
    int Port,
    int MaxPlayers,
    int PlayerCount,
    string? Version,
    TimeSpan? Uptime,
    string InstallPath,
    string WorldPath,
    string ConfigPath,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastStoppedAt,
    IReadOnlyDictionary<string, string> Configuration,
    ContainerStatus ContainerStatus = ContainerStatus.Unknown,
    string? ContainerId = null,
    string? DockerComposeFilePath = null,
    bool HasValidDockerCompose = false
);
