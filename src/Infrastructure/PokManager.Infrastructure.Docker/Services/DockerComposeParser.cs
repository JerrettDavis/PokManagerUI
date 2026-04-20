using PokManager.Infrastructure.Docker.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PokManager.Infrastructure.Docker.Services;

/// <summary>
/// Parses docker-compose YAML files to extract ARK server configuration.
/// </summary>
public class DockerComposeParser
{
    public DockerComposeConfig? ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var yaml = File.ReadAllText(filePath);
            return ParseYaml(yaml, filePath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public DockerComposeConfig? ParseYaml(string yaml, string filePath = "")
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var doc = deserializer.Deserialize<Dictionary<string, object>>(yaml);

            if (!doc.ContainsKey("services"))
                return null;

            var services = (Dictionary<object, object>)doc["services"];
            var firstService = services.Values.FirstOrDefault();

            if (firstService == null)
                return null;

            var service = (Dictionary<object, object>)firstService;

            var config = new DockerComposeConfig
            {
                ConfigFilePath = filePath
            };

            // Extract container name
            if (service.ContainsKey("container_name"))
            {
                config.ContainerName = service["container_name"]?.ToString()?.Trim() ?? "";
            }

            // Extract memory limit
            if (service.ContainsKey("mem_limit"))
            {
                config.MemoryLimit = service["mem_limit"]?.ToString()?.Trim() ?? "";
            }

            // Extract environment variables
            if (service.ContainsKey("environment"))
            {
                var env = (List<object>)service["environment"];
                foreach (var item in env)
                {
                    var envVar = item.ToString() ?? "";
                    var parts = envVar.Split('=', 2);
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim().TrimStart('-', ' ');
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case "INSTANCE_NAME":
                            config.InstanceName = value;
                            break;
                        case "SESSION_NAME":
                            config.SessionName = value;
                            break;
                        case "MAP_NAME":
                            config.MapName = value;
                            break;
                        case "ASA_PORT":
                            if (int.TryParse(value, out var port))
                                config.Port = port;
                            break;
                        case "RCON_PORT":
                            if (int.TryParse(value, out var rconPort))
                                config.RconPort = rconPort;
                            break;
                        case "MAX_PLAYERS":
                            if (int.TryParse(value, out var maxPlayers))
                                config.MaxPlayers = maxPlayers;
                            break;
                        case "SERVER_PASSWORD":
                            config.ServerPassword = value;
                            break;
                        case "SERVER_ADMIN_PASSWORD":
                            config.AdminPassword = value;
                            break;
                        case "BATTLEEYE":
                            config.BattleEyeEnabled = value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "API":
                            config.ApiEnabled = value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "RCON_ENABLED":
                            config.RconEnabled = value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "CLUSTER_ID":
                            config.ClusterId = value;
                            break;
                        case "MOD_IDS":
                            if (!string.IsNullOrWhiteSpace(value))
                                config.ModIds = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(m => m.Trim())
                                    .ToList();
                            break;
                        case "PASSIVE_MODS":
                            if (!string.IsNullOrWhiteSpace(value))
                                config.PassiveMods = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(m => m.Trim())
                                    .ToList();
                            break;
                        case "MOTD":
                            config.Motd = value;
                            break;
                        case "MOTD_DURATION":
                            if (int.TryParse(value, out var motdDuration))
                                config.MotdDuration = motdDuration;
                            break;
                        case "CUSTOM_SERVER_ARGS":
                            config.CustomServerArgs = value;
                            break;
                        case "UPDATE_SERVER":
                            config.UpdateServer = value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "CHECK_FOR_UPDATE_INTERVAL":
                            if (int.TryParse(value, out var updateInterval))
                                config.CheckForUpdateInterval = updateInterval;
                            break;
                        case "UPDATE_WINDOW_MINIMUM_TIME":
                            config.UpdateWindowMinimum = value;
                            break;
                        case "UPDATE_WINDOW_MAXIMUM_TIME":
                            config.UpdateWindowMaximum = value;
                            break;
                        case "RESTART_NOTICE_MINUTES":
                            if (int.TryParse(value, out var restartNotice))
                                config.RestartNoticeMinutes = restartNotice;
                            break;
                        case "TZ":
                            config.TimeZone = value;
                            break;
                    }
                }
            }

            return config;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
