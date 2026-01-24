namespace PokManager.Application.UseCases.InstanceLifecycle.CreateInstance;

public record CreateInstanceRequest(
    string InstanceId,
    string SessionName,
    string MapName,
    int MaxPlayers,
    string ServerAdminPassword,
    string? ServerPassword = null,
    int GamePort = 7777,
    int RconPort = 27020,
    string? ClusterId = null,
    bool EnableApi = false,
    bool EnableBattlEye = true,
    string CorrelationId = null!);
