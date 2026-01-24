namespace PokManager.Infrastructure.Docker.Models;

public class ContainerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public List<PortMapping> Ports { get; set; } = new();
    public Dictionary<string, string> Labels { get; set; } = new();
}

public class PortMapping
{
    public int PrivatePort { get; set; }
    public int PublicPort { get; set; }
    public string Type { get; set; } = string.Empty;
}
