namespace PokManager.Application.UseCases.InstanceLifecycle.StartInstance;

public record StartInstanceRequest(
    string InstanceId,
    string CorrelationId);
