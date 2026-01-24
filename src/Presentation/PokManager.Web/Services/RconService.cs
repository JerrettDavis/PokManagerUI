using CoreRCON;
using System.Text.RegularExpressions;

namespace PokManager.Web.Services;

/// <summary>
/// Service for connecting to ARK servers via RCON and querying player information.
/// </summary>
public class RconService
{
    private readonly ILogger<RconService> _logger;

    public RconService(ILogger<RconService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets online players from an ARK server via RCON.
    /// </summary>
    public async Task<List<InstanceDataCache.PlayerInfo>> GetOnlinePlayersAsync(
        string instanceId, 
        CancellationToken cancellationToken = default)
    {
        var config = RconConfiguration.GetByInstanceId(instanceId);
        if (config == null)
        {
            _logger.LogWarning("No RCON configuration found for instance {InstanceId}", instanceId);
            return new List<InstanceDataCache.PlayerInfo>();
        }

        try
        {
            using var rcon = new RCON(
                System.Net.IPAddress.Parse(config.Host), 
                (ushort)config.Port, 
                config.Password,
                timeout: 5000);

            await rcon.ConnectAsync();

            // Execute ListPlayers command
            var response = await rcon.SendCommandAsync("ListPlayers");
            
            _logger.LogDebug("RCON response from {Instance}: {Response}", instanceId, response);

            return ParsePlayerList(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to RCON for instance {InstanceId} at {Host}:{Port}", 
                instanceId, config.Host, config.Port);
            return new List<InstanceDataCache.PlayerInfo>();
        }
    }

    /// <summary>
    /// Sends a broadcast message to all players on the server.
    /// </summary>
    public async Task<bool> BroadcastMessageAsync(
        string instanceId, 
        string message,
        CancellationToken cancellationToken = default)
    {
        var config = RconConfiguration.GetByInstanceId(instanceId);
        if (config == null) return false;

        try
        {
            using var rcon = new RCON(
                System.Net.IPAddress.Parse(config.Host), 
                (ushort)config.Port, 
                config.Password);

            await rcon.ConnectAsync();
            await rcon.SendCommandAsync($"Broadcast {message}");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting message to {InstanceId}", instanceId);
            return false;
        }
    }

    /// <summary>
    /// Executes a custom RCON command on the server.
    /// </summary>
    public async Task<string?> ExecuteCommandAsync(
        string instanceId, 
        string command,
        CancellationToken cancellationToken = default)
    {
        var config = RconConfiguration.GetByInstanceId(instanceId);
        if (config == null) return null;

        try
        {
            using var rcon = new RCON(
                System.Net.IPAddress.Parse(config.Host), 
                (ushort)config.Port, 
                config.Password);

            await rcon.ConnectAsync();
            return await rcon.SendCommandAsync(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing RCON command on {InstanceId}", instanceId);
            return null;
        }
    }

    private List<InstanceDataCache.PlayerInfo> ParsePlayerList(string response)
    {
        var players = new List<InstanceDataCache.PlayerInfo>();

        if (string.IsNullOrWhiteSpace(response))
            return players;

        // ARK ListPlayers response format:
        // No Players Connected
        // OR
        // 0. PlayerName, 76561198012345678
        // 1. AnotherPlayer, 76561198087654321

        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Skip header lines
            if (line.Contains("No Players") || line.Contains("Players Connected"))
                continue;

            // Parse player line: "0. PlayerName, 76561198012345678"
            var match = Regex.Match(line, @"^\d+\.\s*(.+?),\s*(\d+)");
            if (match.Success)
            {
                players.Add(new InstanceDataCache.PlayerInfo
                {
                    Name = match.Groups[1].Value.Trim(),
                    SteamId = match.Groups[2].Value.Trim(),
                    JoinedAt = DateTime.UtcNow, // We don't get join time from RCON, estimate
                    Level = 0, // Would need separate command to get level
                    Location = null
                });
            }
        }

        return players;
    }
}
