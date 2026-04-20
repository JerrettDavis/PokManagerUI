namespace PokManager.Infrastructure.Docker.Models;

/// <summary>
/// Represents the configuration from a docker-compose file for an ARK server instance.
/// </summary>
public class DockerComposeConfig
{
    public string InstanceName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
    public int Port { get; set; }
    public int RconPort { get; set; }
    public int MaxPlayers { get; set; }
    public string ServerPassword { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public bool BattleEyeEnabled { get; set; }
    public bool ApiEnabled { get; set; }
    public bool RconEnabled { get; set; }
    public string ClusterId { get; set; } = string.Empty;
    public List<string> ModIds { get; set; } = new();
    public List<string> PassiveMods { get; set; } = new();
    public string Motd { get; set; } = string.Empty;
    public int MotdDuration { get; set; }
    public string CustomServerArgs { get; set; } = string.Empty;
    public bool UpdateServer { get; set; }
    public int CheckForUpdateInterval { get; set; }
    public string UpdateWindowMinimum { get; set; } = string.Empty;
    public string UpdateWindowMaximum { get; set; } = string.Empty;
    public int RestartNoticeMinutes { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public string MemoryLimit { get; set; } = string.Empty;

    /// <summary>
    /// Path to the docker-compose file
    /// </summary>
    public string ConfigFilePath { get; set; } = string.Empty;
}
