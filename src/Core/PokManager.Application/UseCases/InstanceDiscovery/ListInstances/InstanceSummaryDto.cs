using PokManager.Domain.Enumerations;

namespace PokManager.Application.UseCases.InstanceDiscovery.ListInstances;

/// <summary>
/// Represents a summary of an instance for list operations.
/// </summary>
/// <param name="Id">The unique identifier of the instance.</param>
/// <param name="State">The current state of the instance.</param>
/// <param name="Health">The health status of the instance process.</param>
/// <param name="LastStartedAt">When the instance was last started, if applicable.</param>
/// <param name="ContainerId">The container ID if the instance is running in a container.</param>
/// <param name="ContainerStatus">The status of the Docker container (e.g., Running, Stopped, NotCreated).</param>
/// <param name="HasValidDockerCompose">Indicates whether a valid docker-compose file exists for this instance.</param>
/// <param name="DockerComposeFilePath">The path to the docker-compose file, if it exists.</param>
public record InstanceSummaryDto(
    string Id,
    InstanceState State,
    ProcessHealth Health,
    DateTimeOffset? LastStartedAt,
    string? ContainerId,
    ContainerStatus ContainerStatus = ContainerStatus.Unknown,
    bool HasValidDockerCompose = false,
    string? DockerComposeFilePath = null
);
