namespace PokManager.Application.UseCases.InstanceQuery;

/// <summary>
/// Request to get the current status of a specific instance.
/// </summary>
/// <param name="InstanceId">The unique identifier of the instance.</param>
/// <param name="CorrelationId">The correlation identifier for tracking this request.</param>
public record GetInstanceStatusRequest(
    string InstanceId,
    string CorrelationId);
