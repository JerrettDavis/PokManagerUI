using PokManager.Application.Ports;
using PokManager.Application.UseCases.InstanceLifecycle.CreateContainer;
using PokManager.Application.UseCases.InstanceLifecycle.DestroyContainer;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.InstanceLifecycle.RecreateContainer;

/// <summary>
/// Handler for recreating a Docker container (destroy + create with data preservation).
/// This is useful for refreshing container configuration without losing data.
/// </summary>
public class RecreateContainerHandler
{
    private readonly DestroyContainerHandler _destroyHandler;
    private readonly CreateContainerHandler _createHandler;

    public RecreateContainerHandler(
        DestroyContainerHandler destroyHandler,
        CreateContainerHandler createHandler)
    {
        _destroyHandler = destroyHandler;
        _createHandler = createHandler;
    }

    public async Task<Result<RecreateContainerResponse>> Handle(
        RecreateContainerRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Destroy existing container (preserve data)
        var destroyRequest = new DestroyContainerRequest(
            InstanceId: request.InstanceId,
            CorrelationId: request.CorrelationId,
            PreserveData: true);

        var destroyResult = await _destroyHandler.Handle(destroyRequest, cancellationToken);
        if (destroyResult.IsFailure)
        {
            return Result.Failure<RecreateContainerResponse>(
                $"Failed to destroy existing container: {destroyResult.Error}");
        }

        // 2. Wait briefly for Docker cleanup
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        // 3. Create new container
        var createRequest = new CreateContainerRequest(
            InstanceId: request.InstanceId,
            CorrelationId: request.CorrelationId,
            AutoStart: request.AutoStart);

        var createResult = await _createHandler.Handle(createRequest, cancellationToken);
        if (createResult.IsFailure)
        {
            return Result.Failure<RecreateContainerResponse>(
                $"Failed to create new container: {createResult.Error}");
        }

        var response = new RecreateContainerResponse(
            InstanceId: request.InstanceId,
            Success: true,
            Message: "Container recreated successfully with preserved data"
        );

        return Result<RecreateContainerResponse>.Success(response);
    }
}
