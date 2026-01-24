namespace PokManager.Infrastructure.PokManager.Models;

/// <summary>
/// Raw status output from POK Manager status command.
/// This is the parsed representation of what the pok.sh status command returns.
/// </summary>
public sealed class PokManagerStatusOutput
{
    /// <summary>
    /// Gets or sets the instance identifier.
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw status string from POK Manager.
    /// Expected values: "running", "stopped", "unknown", etc.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container ID if running.
    /// </summary>
    public string? ContainerId { get; set; }

    /// <summary>
    /// Gets or sets the health status.
    /// Expected values: "healthy", "unhealthy", "starting", "none", etc.
    /// </summary>
    public string? Health { get; set; }

    /// <summary>
    /// Gets or sets the uptime string from the status output.
    /// </summary>
    public string? Uptime { get; set; }

    /// <summary>
    /// Gets or sets any additional raw output from the command.
    /// </summary>
    public string? RawOutput { get; set; }
}
