namespace PokManager.Application.UseCases.InstanceLifecycle.StopInstance;

public record StopInstanceRequest(
    string InstanceId,
    string CorrelationId,
    bool ForceKill = false,
    int TimeoutSeconds = 30);
