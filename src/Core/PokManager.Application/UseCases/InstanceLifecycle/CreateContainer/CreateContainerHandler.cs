using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.InstanceLifecycle.CreateContainer;

/// <summary>
/// Handler for creating a Docker container for an existing instance.
/// </summary>
public class CreateContainerHandler
{
    private readonly IInstanceDiscoveryService _discoveryService;
    private readonly IDockerComposeService _dockerComposeService;

    public CreateContainerHandler(
        IInstanceDiscoveryService discoveryService,
        IDockerComposeService dockerComposeService)
    {
        _discoveryService = discoveryService;
        _dockerComposeService = dockerComposeService;
    }

    public async Task<Result<CreateContainerResponse>> Handle(
        CreateContainerRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Verify instance exists on disk
        var existsResult = await _discoveryService.ExistsAsync(request.InstanceId, cancellationToken);
        if (!existsResult)
        {
            return Result.Failure<CreateContainerResponse>(
                $"Instance '{request.InstanceId}' not found on disk");
        }

        // 2. Locate docker-compose file
        var basePath = "/home/pokuser/asa_server";
        var instancePath = Path.Combine(basePath, $"Instance_{request.InstanceId}");
        var dockerComposePath = Path.Combine(instancePath, $"docker-compose-{request.InstanceId}.yaml");

        if (!File.Exists(dockerComposePath))
        {
            return Result.Failure<CreateContainerResponse>(
                $"Docker compose file not found: {dockerComposePath}");
        }

        // 3. Validate docker-compose file
        var validateResult = await _dockerComposeService.ValidateAsync(dockerComposePath, cancellationToken);
        if (validateResult.IsFailure)
        {
            return Result.Failure<CreateContainerResponse>(
                $"Docker compose file validation failed: {validateResult.Error}");
        }

        // 4. Execute docker-compose up
        var upResult = await _dockerComposeService.UpAsync(dockerComposePath, cancellationToken);
        if (upResult.IsFailure)
        {
            return Result.Failure<CreateContainerResponse>(
                $"Failed to create container: {upResult.Error}");
        }

        var response = new CreateContainerResponse(
            InstanceId: request.InstanceId,
            ContainerId: null, // We don't have container ID immediately, would need to query Docker
            Success: true,
            Message: "Container created and started successfully"
        );

        return Result<CreateContainerResponse>.Success(response);
    }
}
