using PokManager.Infrastructure.Docker.Models;

namespace PokManager.Infrastructure.Docker.Services;

public interface IDockerService
{
    Task<List<ContainerInfo>> ListContainersAsync(CancellationToken cancellationToken = default);
    Task<ContainerInfo?> GetContainerAsync(string nameOrId, CancellationToken cancellationToken = default);
    Task<bool> StartContainerAsync(string nameOrId, CancellationToken cancellationToken = default);
    Task<bool> StopContainerAsync(string nameOrId, CancellationToken cancellationToken = default);
    Task<bool> RestartContainerAsync(string nameOrId, CancellationToken cancellationToken = default);
    Task<string> GetContainerLogsAsync(string nameOrId, int lines = 100, CancellationToken cancellationToken = default);
}
