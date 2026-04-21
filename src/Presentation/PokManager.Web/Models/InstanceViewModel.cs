namespace PokManager.Web.Models;

public class InstanceViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public InstanceStatus Status { get; set; }
    public InstanceHealth Health { get; set; }
    public string Uptime { get; set; } = "N/A";
    public DateTime? StartedAt { get; set; }

    // Map and Server Info
    public string ServerMap { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public int CurrentPlayers { get; set; }
    public int Port { get; set; }
    public int RconPort { get; set; }
    public string Version { get; set; } = string.Empty;

    // Game Settings
    public bool IsPublic { get; set; }
    public bool IsPvE { get; set; }
    public string ServerPassword { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;

    // Mods and Plugins
    public List<string> Mods { get; set; } = new();
    public List<string> ModIds { get; set; } = new();
    public List<string> PassiveMods { get; set; } = new();

    // Server Features
    public bool BattleEyeEnabled { get; set; }
    public bool ApiEnabled { get; set; }
    public bool RconEnabled { get; set; }
    public string ClusterId { get; set; } = string.Empty;

    // MOTD and Messages
    public string Motd { get; set; } = string.Empty;
    public int MotdDuration { get; set; }

    // Update Settings
    public bool UpdateServer { get; set; }
    public int CheckForUpdateInterval { get; set; }
    public string UpdateWindowMinimum { get; set; } = string.Empty;
    public string UpdateWindowMaximum { get; set; } = string.Empty;
    public int RestartNoticeMinutes { get; set; }

    // Advanced
    public string CustomServerArgs { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public string MemoryLimit { get; set; } = string.Empty;

    // Resource Usage (from Docker)
    public long MemoryUsageMB { get; set; }
    public double CpuUsagePercent { get; set; }
}

public enum InstanceStatus
{
    Running,
    Stopped,
    Starting,
    Stopping,
    Error,
    Unknown
}

public enum InstanceHealth
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
