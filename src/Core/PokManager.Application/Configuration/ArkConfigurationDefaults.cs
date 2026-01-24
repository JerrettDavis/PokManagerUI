namespace PokManager.Application.Configuration;

/// <summary>
/// Defines default values for ARK: Survival Ascended server configuration settings.
/// These defaults ensure all common settings are visible in the UI even when not explicitly set in GameUserSettings.ini.
/// </summary>
public static class ArkConfigurationDefaults
{
    /// <summary>
    /// Gets all default ARK configuration values with their display categories.
    /// </summary>
    public static Dictionary<string, ArkConfigSetting> GetDefaults()
    {
        return new Dictionary<string, ArkConfigSetting>
        {
            // Server Information
            // Note: SessionName, ServerMap, MaxPlayers, RCONPort values come from Docker environment variables and override these defaults
            { "SessionName", new ArkConfigSetting("", "Server Information", "The name of your server as it appears in the server list") },
            { "ServerPassword", new ArkConfigSetting("", "Server Information", "Password required to join the server (leave empty for no password)") },
            { "ServerAdminPassword", new ArkConfigSetting("", "Server Information", "Admin password for RCON and admin commands") },
            { "MaxPlayers", new ArkConfigSetting("70", "Server Information", "Maximum number of players allowed on the server") },
            { "ServerMap", new ArkConfigSetting("TheIsland_WP", "Server Information", "The map to load for this server") },
            { "RCONEnabled", new ArkConfigSetting("True", "Server Information", "Enable RCON for remote administration") },
            { "RCONPort", new ArkConfigSetting("27020", "Server Information", "Port for RCON connections") },

            // Gameplay Rates & Multipliers
            { "TamingSpeedMultiplier", new ArkConfigSetting("1.0", "Rates & Multipliers", "Multiplier for taming speed (higher = faster taming)") },
            { "HarvestAmountMultiplier", new ArkConfigSetting("1.0", "Rates & Multipliers", "Multiplier for resource harvest amounts") },
            { "HarvestHealthMultiplier", new ArkConfigSetting("1.0", "Rates & Multipliers", "Multiplier for resource node health") },
            { "XPMultiplier", new ArkConfigSetting("1.0", "Rates & Multipliers", "Overall experience gain multiplier") },
            { "KillXPMultiplier", new ArkConfigSetting("1.0", "Rates & Multipliers", "Experience multiplier for killing creatures") },
            { "HarvestXPMultiplier", new ArkConfigSetting("1.0", "Rates & Multipliers", "Experience multiplier for harvesting resources") },
            { "CraftXPMultiplier", new ArkConfigSetting("1.0", "Rates & Multipliers", "Experience multiplier for crafting items") },
            { "GenericXPMultiplier", new ArkConfigSetting("1.0", "Rates & Multipliers", "Generic experience multiplier") },
            { "SpecialXPMultiplier", new ArkConfigSetting("1.0", "Rates & Multipliers", "Special experience multiplier") },
            
            // Difficulty Settings
            { "DifficultyOffset", new ArkConfigSetting("1.0", "Difficulty", "Difficulty offset (affects max wild dino levels)") },
            { "OverrideOfficialDifficulty", new ArkConfigSetting("5.0", "Difficulty", "Override for official difficulty setting") },
            
            // Player Stats Multipliers
            { "PlayerCharacterHealthMultiplier", new ArkConfigSetting("1.0", "Player Stats", "Multiplier for player health") },
            { "PlayerCharacterStaminaMultiplier", new ArkConfigSetting("1.0", "Player Stats", "Multiplier for player stamina") },
            { "PlayerCharacterWaterMultiplier", new ArkConfigSetting("1.0", "Player Stats", "Multiplier for player water consumption") },
            { "PlayerCharacterFoodMultiplier", new ArkConfigSetting("1.0", "Player Stats", "Multiplier for player food consumption") },
            { "PlayerDamageMultiplier", new ArkConfigSetting("1.0", "Player Stats", "Multiplier for player damage output") },
            { "PlayerResistanceMultiplier", new ArkConfigSetting("1.0", "Player Stats", "Multiplier for player damage resistance") },
            
            // Dino Stats Multipliers
            { "DinoCharacterHealthMultiplier", new ArkConfigSetting("1.0", "Dino Stats", "Multiplier for dinosaur health") },
            { "DinoCharacterStaminaMultiplier", new ArkConfigSetting("1.0", "Dino Stats", "Multiplier for dinosaur stamina") },
            { "DinoCharacterFoodMultiplier", new ArkConfigSetting("1.0", "Dino Stats", "Multiplier for dinosaur food consumption") },
            { "DinoDamageMultiplier", new ArkConfigSetting("1.0", "Dino Stats", "Multiplier for dinosaur damage output") },
            { "DinoResistanceMultiplier", new ArkConfigSetting("1.0", "Dino Stats", "Multiplier for dinosaur damage resistance") },
            
            // Taming & Breeding
            { "MatingIntervalMultiplier", new ArkConfigSetting("1.0", "Taming & Breeding", "Multiplier for mating interval (lower = more frequent mating)") },
            { "EggHatchSpeedMultiplier", new ArkConfigSetting("1.0", "Taming & Breeding", "Multiplier for egg hatching speed") },
            { "BabyMatureSpeedMultiplier", new ArkConfigSetting("1.0", "Taming & Breeding", "Multiplier for baby creature maturation speed") },
            { "BabyFoodConsumptionSpeedMultiplier", new ArkConfigSetting("1.0", "Taming & Breeding", "Multiplier for baby food consumption rate") },
            { "MatingSpeedMultiplier", new ArkConfigSetting("1.0", "Taming & Breeding", "Multiplier for mating speed") },
            { "LayEggIntervalMultiplier", new ArkConfigSetting("1.0", "Taming & Breeding", "Multiplier for egg laying interval") },
            { "BabyCuddleIntervalMultiplier", new ArkConfigSetting("1.0", "Taming & Breeding", "Multiplier for baby cuddle interval") },
            { "BabyImprintAmountMultiplier", new ArkConfigSetting("1.0", "Taming & Breeding", "Multiplier for imprinting effectiveness") },
            { "BabyImprintingStatScaleMultiplier", new ArkConfigSetting("1.0", "Taming & Breeding", "Multiplier for imprinting stat bonus") },
            
            // Resources & Spoiling
            { "ResourcesRespawnPeriodMultiplier", new ArkConfigSetting("1.0", "Resources", "Multiplier for resource respawn time (lower = faster respawn)") },
            { "CropGrowthSpeedMultiplier", new ArkConfigSetting("1.0", "Resources", "Multiplier for crop growth speed") },
            { "CropDecaySpeedMultiplier", new ArkConfigSetting("1.0", "Resources", "Multiplier for crop decay speed") },
            { "GlobalItemDecompositionTimeMultiplier", new ArkConfigSetting("1.0", "Resources", "Multiplier for item spoilage time") },
            { "GlobalCorpseDecompositionTimeMultiplier", new ArkConfigSetting("1.0", "Resources", "Multiplier for corpse decomposition time") },
            
            // Server Rules
            { "PvPEnabled", new ArkConfigSetting("False", "Server Rules", "Enable Player vs Player combat") },
            { "ServerPVE", new ArkConfigSetting("False", "Server Rules", "Enable PvE mode (disables PvP)") },
            { "AllowThirdPersonPlayer", new ArkConfigSetting("True", "Server Rules", "Allow third-person camera view") },
            { "ShowMapPlayerLocation", new ArkConfigSetting("True", "Server Rules", "Show player location on map") },
            { "NoTributeDownloads", new ArkConfigSetting("False", "Server Rules", "Disable tribute downloads from other servers") },
            { "AllowFlyerCarryPvE", new ArkConfigSetting("True", "Server Rules", "Allow flyers to carry creatures in PvE") },
            { "DisableStructureDecayPvE", new ArkConfigSetting("False", "Server Rules", "Disable structure decay in PvE") },
            { "AlwaysNotifyPlayerLeft", new ArkConfigSetting("True", "Server Rules", "Always notify when a player leaves") },
            { "DontAlwaysNotifyPlayerJoined", new ArkConfigSetting("False", "Server Rules", "Don't always notify when a player joins") },
            { "ServerHardcore", new ArkConfigSetting("False", "Server Rules", "Enable hardcore mode (character deletion on death)") },
            { "ServerCrosshair", new ArkConfigSetting("True", "Server Rules", "Enable crosshair") },
            { "ServerForceNoHUD", new ArkConfigSetting("False", "Server Rules", "Force HUD to be disabled") },
            { "AllowCaveBuildingPvE", new ArkConfigSetting("False", "Server Rules", "Allow building in caves in PvE") },
            { "EnablePvPGamma", new ArkConfigSetting("False", "Server Rules", "Enable gamma adjustment in PvP") },
            
            // Structure & Limits
            { "MaxTamedDinos", new ArkConfigSetting("5000", "Limits", "Maximum number of tamed creatures per tribe") },
            { "MaxNumbersofPlayersInTribe", new ArkConfigSetting("0", "Limits", "Maximum players per tribe (0 = unlimited)") },
            { "MaxStructuresInRange", new ArkConfigSetting("10500", "Limits", "Maximum structures allowed in a certain range") },
            { "StructurePreventResourceRadiusMultiplier", new ArkConfigSetting("1.0", "Limits", "Multiplier for structure resource prevention radius") },
            { "PlatformSaddleBuildAreaBoundsMultiplier", new ArkConfigSetting("1.0", "Limits", "Multiplier for platform saddle build area") },
            
            // Time & Day Cycle
            { "DayTimeSpeedScale", new ArkConfigSetting("1.0", "Time & Day Cycle", "Multiplier for daytime speed") },
            { "NightTimeSpeedScale", new ArkConfigSetting("1.0", "Time & Day Cycle", "Multiplier for nighttime speed") },
            { "DayCycleSpeedScale", new ArkConfigSetting("1.0", "Time & Day Cycle", "Multiplier for overall day/night cycle speed") },
            
            // Save & Performance
            { "AutoSavePeriodMinutes", new ArkConfigSetting("15.0", "Performance", "Minutes between automatic world saves") },
            { "PerPlatformMaxStructuresMultiplier", new ArkConfigSetting("1.0", "Performance", "Multiplier for max structures per platform") },
            { "MaxPlatformSaddleStructureLimit", new ArkConfigSetting("0", "Performance", "Maximum structures on platform saddles (0 = use default)") },
            
            // Tribe & Player Settings
            { "AllowRaidDinoFeeding", new ArkConfigSetting("False", "Tribe Settings", "Allow feeding enemy tribe's dinos during raids") },
            { "RaidDinoCharacterFoodDrainMultiplier", new ArkConfigSetting("1.0", "Tribe Settings", "Food drain multiplier for dinos during raids") },
            { "PvEDinoDecayPeriodMultiplier", new ArkConfigSetting("1.0", "Tribe Settings", "Multiplier for dino decay period in PvE") },
            { "PvEStructureDecayPeriodMultiplier", new ArkConfigSetting("1.0", "Tribe Settings", "Multiplier for structure decay period in PvE") },
            
            // Custom Game Modes (ASA Specific)
            { "bDisableFriendlyFire", new ArkConfigSetting("False", "Advanced", "Disable friendly fire") },
            { "bPvEDisableFriendlyFire", new ArkConfigSetting("False", "Advanced", "Disable friendly fire in PvE") },
            { "bEnablePvPGamma", new ArkConfigSetting("False", "Advanced", "Enable gamma in PvP") },
            { "bDisablePvEGamma", new ArkConfigSetting("False", "Advanced", "Disable gamma in PvE") },
            { "bAllowUnlimitedRespecs", new ArkConfigSetting("False", "Advanced", "Allow unlimited character respecs") },
            { "bDisableDinoDecayPvE", new ArkConfigSetting("False", "Advanced", "Disable dino decay in PvE") },
            { "bUseSingleplayerSettings", new ArkConfigSetting("False", "Advanced", "Use singleplayer settings multipliers") },
        };
    }
    
    /// <summary>
    /// Represents an ARK configuration setting with metadata.
    /// </summary>
    public class ArkConfigSetting
    {
        public string DefaultValue { get; }
        public string Category { get; }
        public string Description { get; }
        
        public ArkConfigSetting(string defaultValue, string category, string description)
        {
            DefaultValue = defaultValue;
            Category = category;
            Description = description;
        }
    }
}
