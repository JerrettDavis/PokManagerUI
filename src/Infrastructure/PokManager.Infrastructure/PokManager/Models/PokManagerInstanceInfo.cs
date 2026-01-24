namespace PokManager.Infrastructure.PokManager.Models;

/// <summary>
/// Raw parsed instance information from POK Manager filesystem.
/// This represents the basic info we can get from Instance_* directories.
/// </summary>
public sealed class PokManagerInstanceInfo
{
    /// <summary>
    /// Gets or sets the instance identifier.
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full directory path for this instance.
    /// </summary>
    public string DirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the instance directory exists.
    /// </summary>
    public bool Exists { get; set; }
}
