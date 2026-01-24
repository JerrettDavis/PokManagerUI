namespace PokManager.Infrastructure.Docker.Models;

/// <summary>
/// Represents an instance discovered from disk (Instance_* directory).
/// </summary>
public class InstanceDiskInfo
{
    public required string InstanceId { get; init; }
    public required string DirectoryPath { get; init; }
    public string? DockerComposeFilePath { get; init; }
    public bool HasDockerCompose => !string.IsNullOrEmpty(DockerComposeFilePath);
    public bool HasGameUserSettings { get; init; }
    public bool HasSavedData { get; init; }
    public DateTime DirectoryCreated { get; init; }
    public long DirectorySizeBytes { get; init; }
}
