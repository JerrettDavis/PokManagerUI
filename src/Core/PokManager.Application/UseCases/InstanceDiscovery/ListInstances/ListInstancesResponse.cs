namespace PokManager.Application.UseCases.InstanceDiscovery.ListInstances;

/// <summary>
/// Response containing a list of instance summaries.
/// </summary>
/// <param name="Instances">Read-only list of instance summaries.</param>
public record ListInstancesResponse(
    IReadOnlyList<InstanceSummaryDto> Instances
);
