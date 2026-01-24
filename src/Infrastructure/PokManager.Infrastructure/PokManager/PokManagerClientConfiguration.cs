namespace PokManager.Infrastructure.PokManager;

/// <summary>
/// Configuration settings for the PokManagerClient.
/// Contains paths and settings needed to interact with the POK Manager bash script.
/// </summary>
public sealed class PokManagerClientConfiguration
{
    /// <summary>
    /// Gets or sets the full path to the pok.sh script.
    /// </summary>
    public string PokManagerScriptPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the working directory where commands should be executed.
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default timeout for command execution.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the base path where Instance_* directories are located.
    /// </summary>
    public string InstancesBasePath { get; set; } = string.Empty;

    /// <summary>
    /// Validates the configuration and throws an exception if invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(PokManagerScriptPath))
        {
            errors.Add("PokManagerScriptPath is required.");
        }

        if (string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            errors.Add("WorkingDirectory is required.");
        }

        if (string.IsNullOrWhiteSpace(InstancesBasePath))
        {
            errors.Add("InstancesBasePath is required.");
        }

        if (DefaultTimeout <= TimeSpan.Zero)
        {
            errors.Add("DefaultTimeout must be greater than zero.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"PokManagerClientConfiguration is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}
