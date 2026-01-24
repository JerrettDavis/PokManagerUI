using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.InstanceDiscovery.ListInstances;

/// <summary>
/// Handler for listing all available Palworld server instances.
/// </summary>
public class ListInstancesHandler(
    IInstanceDiscoveryService discoveryService,
    IPokManagerClient pokManagerClient
)
{
    private readonly IInstanceDiscoveryService _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
    private readonly IPokManagerClient _pokManagerClient = pokManagerClient ?? throw new ArgumentNullException(nameof(pokManagerClient));

    /// <summary>
    /// Handles the list instances request.
    /// </summary>
    /// <param name="request">The list instances request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of instance summaries.</returns>
    public async Task<Result<ListInstancesResponse>> Handle(
        ListInstancesRequest request,
        CancellationToken cancellationToken = default)
    {
        // Discover all instance IDs
        var discoveryResult = await _discoveryService.DiscoverInstancesAsync(cancellationToken);
        if (discoveryResult.IsFailure)
        {
            return Result.Failure<ListInstancesResponse>(discoveryResult.Error);
        }

        var instanceIds = discoveryResult.Value;
        var summaries = new List<InstanceSummaryDto>();

        // Get details for each instance
        foreach (var instanceId in instanceIds)
        {
            var detailsResult = await _pokManagerClient.GetInstanceDetailsAsync(instanceId, cancellationToken);

            // Skip instances that fail to load (they may have been deleted or are in an invalid state)
            if (detailsResult.IsFailure)
            {
                continue;
            }

            var details = detailsResult.Value;
            var summary = new InstanceSummaryDto(
                details.InstanceId,
                details.State,
                details.Health,
                details.LastStartedAt,
                details.ContainerId,
                details.ContainerStatus,
                details.HasValidDockerCompose,
                details.DockerComposeFilePath
            );

            summaries.Add(summary);
        }

        var response = new ListInstancesResponse(summaries);
        return Result<ListInstancesResponse>.Success(response);
    }
}
