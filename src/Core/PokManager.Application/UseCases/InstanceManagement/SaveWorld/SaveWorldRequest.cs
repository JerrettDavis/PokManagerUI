namespace PokManager.Application.UseCases.InstanceManagement.SaveWorld;

/// <summary>
/// Request to save the world state of a running instance.
/// </summary>
/// <param name="InstanceId">The unique identifier of the instance to save.</param>
/// <param name="CorrelationId">The correlation identifier for tracking this request.</param>
public record SaveWorldRequest(
    string InstanceId,
    string CorrelationId);
