namespace PokManager.Web.Models;

/// <summary>
/// View model for displaying server configuration in the UI.
/// </summary>
public class ConfigurationViewModel
{
    public string InstanceId { get; set; } = string.Empty;
    public Dictionary<string, string> Settings { get; set; } = new();
    public DateTimeOffset LastModified { get; set; }
    public bool HasUnsavedChanges { get; set; }
    public bool IsValid { get; set; } = true;
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();

    /// <summary>
    /// Gets a setting value by key, or returns default if not found.
    /// </summary>
    public string GetSetting(string key, string defaultValue = "")
    {
        return Settings.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Sets a setting value.
    /// </summary>
    public void SetSetting(string key, string value)
    {
        Settings[key] = value;
        HasUnsavedChanges = true;
    }

    /// <summary>
    /// Gets commonly accessed configuration properties.
    /// </summary>
    public string ServerName
    {
        get => GetSetting("ServerName", "Palworld Server");
        set => SetSetting("ServerName", value);
    }

    public string ServerDescription
    {
        get => GetSetting("ServerDescription", "");
        set => SetSetting("ServerDescription", value);
    }

    public int MaxPlayers
    {
        get => int.TryParse(GetSetting("MaxPlayers", "32"), out var value) ? value : 32;
        set => SetSetting("MaxPlayers", value.ToString());
    }

    public bool IsPublicServer
    {
        get => bool.TryParse(GetSetting("PublicServer", "false"), out var value) && value;
        set => SetSetting("PublicServer", value.ToString().ToLower());
    }

    public string ServerPassword
    {
        get => GetSetting("ServerPassword", "");
        set => SetSetting("ServerPassword", value);
    }

    public string AdminPassword
    {
        get => GetSetting("AdminPassword", "");
        set => SetSetting("AdminPassword", value);
    }
}
