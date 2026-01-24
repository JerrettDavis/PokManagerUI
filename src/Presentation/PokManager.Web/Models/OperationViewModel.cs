namespace PokManager.Web.Models;

/// <summary>
/// View model for tracking long-running operations in the UI.
/// </summary>
public class OperationViewModel
{
    public string OperationId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public OperationStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int ProgressPercentage { get; set; }
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets whether the operation is still in progress.
    /// </summary>
    public bool IsInProgress => Status == OperationStatus.Running;

    /// <summary>
    /// Gets whether the operation completed successfully.
    /// </summary>
    public bool IsSuccessful => Status == OperationStatus.Completed;

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    public bool IsFailed => Status == OperationStatus.Failed;

    /// <summary>
    /// Gets the duration of the operation.
    /// </summary>
    public TimeSpan Duration
    {
        get
        {
            var endTime = CompletedAt ?? DateTimeOffset.UtcNow;
            return endTime - StartedAt;
        }
    }

    /// <summary>
    /// Gets the formatted duration string.
    /// </summary>
    public string FormattedDuration
    {
        get
        {
            var duration = Duration;
            if (duration.TotalSeconds < 1)
                return "< 1 second";
            if (duration.TotalMinutes < 1)
                return $"{(int)duration.TotalSeconds} seconds";
            if (duration.TotalHours < 1)
                return $"{(int)duration.TotalMinutes} minutes {duration.Seconds} seconds";

            return $"{(int)duration.TotalHours} hours {duration.Minutes} minutes";
        }
    }
}

/// <summary>
/// Status of an operation.
/// </summary>
public enum OperationStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
