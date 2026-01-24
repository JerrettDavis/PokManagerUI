using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.InstanceLifecycle.DestroyContainer;

/// <summary>
/// Handler for destroying (stopping and removing) a Docker container.
/// By default, preserves all disk data for safety.
/// </summary>
public class DestroyContainerHandler
{
    private readonly IInstanceDiscoveryService _discoveryService;
    private readonly IDockerComposeService _dockerComposeService;

    public DestroyContainerHandler(
        IInstanceDiscoveryService discoveryService,
        IDockerComposeService dockerComposeService)
    {
        _discoveryService = discoveryService;
        _dockerComposeService = dockerComposeService;
    }

    public async Task<Result<DestroyContainerResponse>> Handle(
        DestroyContainerRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Verify instance exists on disk
        var existsResult = await _discoveryService.ExistsAsync(request.InstanceId, cancellationToken);
        if (!existsResult)
        {
            return Result.Failure<DestroyContainerResponse>(
                $"Instance '{request.InstanceId}' not found on disk");
        }

        // 2. Locate docker-compose file
        var basePath = "/home/pokuser/asa_server";
        var instancePath = Path.Combine(basePath, $"Instance_{request.InstanceId}");
        var dockerComposePath = Path.Combine(instancePath, $"docker-compose-{request.InstanceId}.yaml");

        if (!File.Exists(dockerComposePath))
        {
            return Result.Failure<DestroyContainerResponse>(
                $"Docker compose file not found: {dockerComposePath}");
        }

        // 3. Execute docker-compose down (with or without volume removal)
        var removeVolumes = !request.PreserveData;
        var downResult = await _dockerComposeService.DownAsync(
            dockerComposePath,
            removeVolumes,
            cancellationToken);

        if (downResult.IsFailure)
        {
            return Result.Failure<DestroyContainerResponse>(
                $"Failed to destroy container: {downResult.Error}");
        }

        var message = request.PreserveData
            ? "Container destroyed successfully. All disk data and volumes preserved."
            : "Container and volumes destroyed successfully. Disk data removed.";

        var response = new DestroyContainerResponse(
            InstanceId: request.InstanceId,
            Success: true,
            DataPreserved: request.PreserveData,
            Message: message
        );

        return Result<DestroyContainerResponse>.Success(response);
    }
}
