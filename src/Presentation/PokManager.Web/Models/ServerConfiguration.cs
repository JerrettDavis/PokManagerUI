namespace PokManager.Web.Models;

/// <summary>
/// Represents the configuration for a Palworld server instance.
/// </summary>
public class ServerConfiguration
{
    // Basic Settings
    public string SessionName { get; set; } = string.Empty;
    public int MaxPlayers { get; set; } = 32;
    public string ServerMap { get; set; } = "TheIsland";
    public string ServerPassword { get; set; } = string.Empty;
    public int ServerPort { get; set; } = 8211;
    public string ServerDescription { get; set; } = string.Empty;

    // Gameplay Settings
    public bool IsPvE { get; set; } = true;
    public bool IsPublic { get; set; } = true;
    public int DifficultyLevel { get; set; } = 5;
    public double DayTimeSpeedRate { get; set; } = 1.0;
    public double NightTimeSpeedRate { get; set; } = 1.0;
    public double ExpRate { get; set; } = 1.0;
    public double PalCaptureRate { get; set; } = 1.0;
    public double PalSpawnNumRate { get; set; } = 1.0;
    public double PalDamageRateAttack { get; set; } = 1.0;
    public double PalDamageRateDefense { get; set; } = 1.0;
    public double PlayerDamageRateAttack { get; set; } = 1.0;
    public double PlayerDamageRateDefense { get; set; } = 1.0;
    public double PlayerStomachDecreaseRate { get; set; } = 1.0;
    public double PlayerStaminaDecreaseRate { get; set; } = 1.0;
    public double PlayerAutoHPRegeneRate { get; set; } = 1.0;
    public double PlayerAutoHpRegeneRateInSleep { get; set; } = 1.0;
    public bool EnablePlayerToPlayerDamage { get; set; } = false;
    public bool EnableFriendlyFire { get; set; } = false;
    public bool EnableInvaderEnemy { get; set; } = true;
    public bool EnableAimAssistPad { get; set; } = true;
    public bool EnableAimAssistKeyboard { get; set; } = false;
    public int DropItemMaxNum { get; set; } = 3000;
    public int BaseCampMaxNum { get; set; } = 128;
    public int BaseCampWorkerMaxNum { get; set; } = 15;
    public bool CanPickupOtherGuildDeathPenaltyDrop { get; set; } = false;
    public bool EnableNonLoginPenalty { get; set; } = true;
    public bool EnableFastTravel { get; set; } = true;
    public bool IsStartLocationSelectByMap { get; set; } = true;
    public bool ExistPlayerAfterLogout { get; set; } = false;
    public bool EnableDefenseOtherGuildPlayer { get; set; } = false;

    // Mods
    public List<string> Mods { get; set; } = new();

    // Advanced Settings (custom key-value pairs)
    public Dictionary<string, string> AdvancedSettings { get; set; } = new();

    /// <summary>
    /// Converts the configuration to a dictionary format expected by the API.
    /// </summary>
    public Dictionary<string, string> ToDictionary()
    {
        var config = new Dictionary<string, string>
        {
            ["SessionName"] = SessionName,
            ["MaxPlayers"] = MaxPlayers.ToString(),
            ["ServerMap"] = ServerMap,
            ["ServerPassword"] = ServerPassword,
            ["ServerPort"] = ServerPort.ToString(),
            ["ServerDescription"] = ServerDescription,
            ["IsPvE"] = IsPvE.ToString(),
            ["IsPublic"] = IsPublic.ToString(),
            ["DifficultyLevel"] = DifficultyLevel.ToString(),
            ["DayTimeSpeedRate"] = DayTimeSpeedRate.ToString("F2"),
            ["NightTimeSpeedRate"] = NightTimeSpeedRate.ToString("F2"),
            ["ExpRate"] = ExpRate.ToString("F2"),
            ["PalCaptureRate"] = PalCaptureRate.ToString("F2"),
            ["PalSpawnNumRate"] = PalSpawnNumRate.ToString("F2"),
            ["PalDamageRateAttack"] = PalDamageRateAttack.ToString("F2"),
            ["PalDamageRateDefense"] = PalDamageRateDefense.ToString("F2"),
            ["PlayerDamageRateAttack"] = PlayerDamageRateAttack.ToString("F2"),
            ["PlayerDamageRateDefense"] = PlayerDamageRateDefense.ToString("F2"),
            ["PlayerStomachDecreaseRate"] = PlayerStomachDecreaseRate.ToString("F2"),
            ["PlayerStaminaDecreaseRate"] = PlayerStaminaDecreaseRate.ToString("F2"),
            ["PlayerAutoHPRegeneRate"] = PlayerAutoHPRegeneRate.ToString("F2"),
            ["PlayerAutoHpRegeneRateInSleep"] = PlayerAutoHpRegeneRateInSleep.ToString("F2"),
            ["EnablePlayerToPlayerDamage"] = EnablePlayerToPlayerDamage.ToString(),
            ["EnableFriendlyFire"] = EnableFriendlyFire.ToString(),
            ["EnableInvaderEnemy"] = EnableInvaderEnemy.ToString(),
            ["EnableAimAssistPad"] = EnableAimAssistPad.ToString(),
            ["EnableAimAssistKeyboard"] = EnableAimAssistKeyboard.ToString(),
            ["DropItemMaxNum"] = DropItemMaxNum.ToString(),
            ["BaseCampMaxNum"] = BaseCampMaxNum.ToString(),
            ["BaseCampWorkerMaxNum"] = BaseCampWorkerMaxNum.ToString(),
            ["CanPickupOtherGuildDeathPenaltyDrop"] = CanPickupOtherGuildDeathPenaltyDrop.ToString(),
            ["EnableNonLoginPenalty"] = EnableNonLoginPenalty.ToString(),
            ["EnableFastTravel"] = EnableFastTravel.ToString(),
            ["IsStartLocationSelectByMap"] = IsStartLocationSelectByMap.ToString(),
            ["ExistPlayerAfterLogout"] = ExistPlayerAfterLogout.ToString(),
            ["EnableDefenseOtherGuildPlayer"] = EnableDefenseOtherGuildPlayer.ToString(),
        };

        // Add mods
        if (Mods.Any())
        {
            config["Mods"] = string.Join(",", Mods);
        }

        // Add advanced settings
        foreach (var (key, value) in AdvancedSettings)
        {
            config[key] = value;
        }

        return config;
    }

    /// <summary>
    /// Creates a ServerConfiguration from a dictionary.
    /// </summary>
    public static ServerConfiguration FromDictionary(IReadOnlyDictionary<string, string> dict)
    {
        var config = new ServerConfiguration();

        if (dict.TryGetValue("SessionName", out var sessionName))
            config.SessionName = sessionName;
        if (dict.TryGetValue("MaxPlayers", out var maxPlayers) && int.TryParse(maxPlayers, out var mp))
            config.MaxPlayers = mp;
        if (dict.TryGetValue("ServerMap", out var serverMap))
            config.ServerMap = serverMap;
        if (dict.TryGetValue("ServerPassword", out var serverPassword))
            config.ServerPassword = serverPassword;
        if (dict.TryGetValue("ServerPort", out var serverPort) && int.TryParse(serverPort, out var sp))
            config.ServerPort = sp;
        if (dict.TryGetValue("ServerDescription", out var serverDescription))
            config.ServerDescription = serverDescription;
        if (dict.TryGetValue("IsPvE", out var isPvE) && bool.TryParse(isPvE, out var pve))
            config.IsPvE = pve;
        if (dict.TryGetValue("IsPublic", out var isPublic) && bool.TryParse(isPublic, out var pub))
            config.IsPublic = pub;
        if (dict.TryGetValue("DifficultyLevel", out var difficulty) && int.TryParse(difficulty, out var diff))
            config.DifficultyLevel = diff;
        if (dict.TryGetValue("DayTimeSpeedRate", out var dayTime) && double.TryParse(dayTime, out var dt))
            config.DayTimeSpeedRate = dt;
        if (dict.TryGetValue("NightTimeSpeedRate", out var nightTime) && double.TryParse(nightTime, out var nt))
            config.NightTimeSpeedRate = nt;
        if (dict.TryGetValue("ExpRate", out var expRate) && double.TryParse(expRate, out var er))
            config.ExpRate = er;
        if (dict.TryGetValue("PalCaptureRate", out var palCapture) && double.TryParse(palCapture, out var pcr))
            config.PalCaptureRate = pcr;
        if (dict.TryGetValue("PalSpawnNumRate", out var palSpawn) && double.TryParse(palSpawn, out var psr))
            config.PalSpawnNumRate = psr;
        if (dict.TryGetValue("PalDamageRateAttack", out var palDmgAtk) && double.TryParse(palDmgAtk, out var pda))
            config.PalDamageRateAttack = pda;
        if (dict.TryGetValue("PalDamageRateDefense", out var palDmgDef) && double.TryParse(palDmgDef, out var pdd))
            config.PalDamageRateDefense = pdd;
        if (dict.TryGetValue("PlayerDamageRateAttack", out var plrDmgAtk) && double.TryParse(plrDmgAtk, out var plda))
            config.PlayerDamageRateAttack = plda;
        if (dict.TryGetValue("PlayerDamageRateDefense", out var plrDmgDef) && double.TryParse(plrDmgDef, out var pldd))
            config.PlayerDamageRateDefense = pldd;
        if (dict.TryGetValue("PlayerStomachDecreaseRate", out var stomach) && double.TryParse(stomach, out var st))
            config.PlayerStomachDecreaseRate = st;
        if (dict.TryGetValue("PlayerStaminaDecreaseRate", out var stamina) && double.TryParse(stamina, out var stm))
            config.PlayerStaminaDecreaseRate = stm;
        if (dict.TryGetValue("PlayerAutoHPRegeneRate", out var hpRegen) && double.TryParse(hpRegen, out var hpr))
            config.PlayerAutoHPRegeneRate = hpr;
        if (dict.TryGetValue("PlayerAutoHpRegeneRateInSleep", out var hpSleep) && double.TryParse(hpSleep, out var hps))
            config.PlayerAutoHpRegeneRateInSleep = hps;
        if (dict.TryGetValue("EnablePlayerToPlayerDamage", out var pvpDmg) && bool.TryParse(pvpDmg, out var pvp))
            config.EnablePlayerToPlayerDamage = pvp;
        if (dict.TryGetValue("EnableFriendlyFire", out var ff) && bool.TryParse(ff, out var ffb))
            config.EnableFriendlyFire = ffb;
        if (dict.TryGetValue("EnableInvaderEnemy", out var invader) && bool.TryParse(invader, out var inv))
            config.EnableInvaderEnemy = inv;
        if (dict.TryGetValue("EnableAimAssistPad", out var aimPad) && bool.TryParse(aimPad, out var ap))
            config.EnableAimAssistPad = ap;
        if (dict.TryGetValue("EnableAimAssistKeyboard", out var aimKb) && bool.TryParse(aimKb, out var ak))
            config.EnableAimAssistKeyboard = ak;
        if (dict.TryGetValue("DropItemMaxNum", out var dropItem) && int.TryParse(dropItem, out var di))
            config.DropItemMaxNum = di;
        if (dict.TryGetValue("BaseCampMaxNum", out var baseCamp) && int.TryParse(baseCamp, out var bc))
            config.BaseCampMaxNum = bc;
        if (dict.TryGetValue("BaseCampWorkerMaxNum", out var worker) && int.TryParse(worker, out var wk))
            config.BaseCampWorkerMaxNum = wk;
        if (dict.TryGetValue("CanPickupOtherGuildDeathPenaltyDrop", out var pickup) && bool.TryParse(pickup, out var pu))
            config.CanPickupOtherGuildDeathPenaltyDrop = pu;
        if (dict.TryGetValue("EnableNonLoginPenalty", out var penalty) && bool.TryParse(penalty, out var pen))
            config.EnableNonLoginPenalty = pen;
        if (dict.TryGetValue("EnableFastTravel", out var fastTravel) && bool.TryParse(fastTravel, out var ft))
            config.EnableFastTravel = ft;
        if (dict.TryGetValue("IsStartLocationSelectByMap", out var startLoc) && bool.TryParse(startLoc, out var sl))
            config.IsStartLocationSelectByMap = sl;
        if (dict.TryGetValue("ExistPlayerAfterLogout", out var logout) && bool.TryParse(logout, out var lo))
            config.ExistPlayerAfterLogout = lo;
        if (dict.TryGetValue("EnableDefenseOtherGuildPlayer", out var defense) && bool.TryParse(defense, out var def))
            config.EnableDefenseOtherGuildPlayer = def;

        // Parse mods
        if (dict.TryGetValue("Mods", out var mods) && !string.IsNullOrWhiteSpace(mods))
        {
            config.Mods = mods.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        // Parse advanced settings (any keys not recognized above)
        var knownKeys = new HashSet<string>
        {
            "SessionName", "MaxPlayers", "ServerMap", "ServerPassword", "ServerPort", "ServerDescription",
            "IsPvE", "IsPublic", "DifficultyLevel", "DayTimeSpeedRate", "NightTimeSpeedRate",
            "ExpRate", "PalCaptureRate", "PalSpawnNumRate", "PalDamageRateAttack", "PalDamageRateDefense",
            "PlayerDamageRateAttack", "PlayerDamageRateDefense", "PlayerStomachDecreaseRate",
            "PlayerStaminaDecreaseRate", "PlayerAutoHPRegeneRate", "PlayerAutoHpRegeneRateInSleep",
            "EnablePlayerToPlayerDamage", "EnableFriendlyFire", "EnableInvaderEnemy",
            "EnableAimAssistPad", "EnableAimAssistKeyboard", "DropItemMaxNum", "BaseCampMaxNum",
            "BaseCampWorkerMaxNum", "CanPickupOtherGuildDeathPenaltyDrop", "EnableNonLoginPenalty",
            "EnableFastTravel", "IsStartLocationSelectByMap", "ExistPlayerAfterLogout",
            "EnableDefenseOtherGuildPlayer", "Mods"
        };

        foreach (var (key, value) in dict)
        {
            if (!knownKeys.Contains(key))
            {
                config.AdvancedSettings[key] = value;
            }
        }

        return config;
    }
}
