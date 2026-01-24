namespace PokManager.Application.BackgroundWorkers;

/// <summary>
/// Types of refresh operations that can be requested.
/// </summary>
public enum RefreshType
{
    InstanceStatus,
    InstanceDetails,
    ContainerMetrics,
    ContainerLogs,
    BackupList,
    Configuration,
    PlayerList,
    TemplateList,
    AllInstanceData // Refresh everything for an instance
}

/// <summary>
/// Represents a request to refresh cached data.
/// </summary>
/// <param name="Type">The type of data to refresh.</param>
/// <param name="InstanceId">The instance ID if applicable.</param>
/// <param name="Priority">If true, skip queue and execute immediately.</param>
/// <param name="CorrelationId">Optional correlation ID for tracking.</param>
public record RefreshRequest(
    RefreshType Type,
    string? InstanceId = null,
    bool Priority = false,
    string? CorrelationId = null
);
