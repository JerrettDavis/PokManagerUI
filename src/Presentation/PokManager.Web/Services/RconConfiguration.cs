namespace PokManager.Web.Services;

/// <summary>
/// Configuration for RCON connections to ARK server instances.
/// </summary>
public class RconConfiguration
{
    /// <summary>
    /// Maps container names to their RCON configuration.
    /// </summary>
    public static readonly Dictionary<string, RconInstanceConfig> Instances = new()
    {
        ["asa_TheLostColonoscopy"] = new RconInstanceConfig
        {
            ContainerName = "asa_TheLostColonoscopy",
            InstanceId = "TheLostColonoscopy",
            Host = "10.0.0.216",
            Port = 27020,
            Password = "jdadminpass!"
        },
        ["asa_RagNRock"] = new RconInstanceConfig
        {
            ContainerName = "asa_RagNRock",
            InstanceId = "RagNRock",
            Host = "10.0.0.216",
            Port = 27021,
            Password = "jdadminpass!"
        },
        ["asa_AbHoorsNation"] = new RconInstanceConfig
        {
            ContainerName = "asa_AbHoorsNation",
            InstanceId = "AbHoorsNation",
            Host = "10.0.0.216",
            Port = 27022,
            Password = "jdadminpass!"
        },
        ["asa_Valderrama"] = new RconInstanceConfig
        {
            ContainerName = "asa_Valderrama",
            InstanceId = "Valderrama",
            Host = "10.0.0.216",
            Port = 27023,
            Password = "jdadminpass!"
        }
    };

    public static RconInstanceConfig? GetByContainerName(string containerName)
    {
        return Instances.TryGetValue(containerName, out var config) ? config : null;
    }

    public static RconInstanceConfig? GetByInstanceId(string instanceId)
    {
        return Instances.Values.FirstOrDefault(c => 
            c.InstanceId.Equals(instanceId, StringComparison.OrdinalIgnoreCase));
    }
}

public class RconInstanceConfig
{
    public string ContainerName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Password { get; set; } = string.Empty;
}
