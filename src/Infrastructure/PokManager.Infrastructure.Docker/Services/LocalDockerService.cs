using System.Diagnostics;
using System.Text.Json;
using PokManager.Infrastructure.Docker.Models;

namespace PokManager.Infrastructure.Docker.Services;

public class LocalDockerService : IDockerService
{
    public async Task<List<ContainerInfo>> ListContainersAsync(CancellationToken cancellationToken = default)
    {
        var output = await ExecuteDockerCommandAsync("ps -a --format json", cancellationToken);

        var containers = new List<ContainerInfo>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(line);
                var root = jsonDoc.RootElement;

                var container = new ContainerInfo
                {
                    Id = root.GetProperty("ID").GetString() ?? "",
                    Name = root.GetProperty("Names").GetString()?.TrimStart('/') ?? "",
                    Image = root.GetProperty("Image").GetString() ?? "",
                    Status = root.GetProperty("Status").GetString() ?? "",
                    State = root.GetProperty("State").GetString() ?? "",
                };

                // Try to parse created date - format may vary
                try
                {
                    var createdStr = root.GetProperty("CreatedAt").GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(createdStr))
                    {
                        // Docker format: "2024-01-19 19:35:46 -0600 CST"
                        if (DateTime.TryParse(createdStr, out var createdDate))
                        {
                            container.Created = createdDate;
                        }
                    }
                }
                catch
                {
                    container.Created = DateTime.UtcNow;
                }

                // Parse ports
                var portsStr = root.GetProperty("Ports").GetString() ?? "";
                container.Ports = ParsePorts(portsStr);

                containers.Add(container);
            }
            catch (Exception ex)
            {
                // Log error but continue processing other containers
                Console.WriteLine($"Error parsing container: {ex.Message}");
            }
        }

        return containers;
    }

    public async Task<ContainerInfo?> GetContainerAsync(string nameOrId, CancellationToken cancellationToken = default)
    {
        var containers = await ListContainersAsync(cancellationToken);
        return containers.FirstOrDefault(c =>
            c.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) ||
            c.Id.StartsWith(nameOrId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> StartContainerAsync(string nameOrId, CancellationToken cancellationToken = default)
    {
        var output = await ExecuteDockerCommandAsync($"start {nameOrId}", cancellationToken);
        return !string.IsNullOrWhiteSpace(output);
    }

    public async Task<bool> StopContainerAsync(string nameOrId, CancellationToken cancellationToken = default)
    {
        var output = await ExecuteDockerCommandAsync($"stop {nameOrId}", cancellationToken);
        return !string.IsNullOrWhiteSpace(output);
    }

    public async Task<bool> RestartContainerAsync(string nameOrId, CancellationToken cancellationToken = default)
    {
        var output = await ExecuteDockerCommandAsync($"restart {nameOrId}", cancellationToken);
        return !string.IsNullOrWhiteSpace(output);
    }

    public async Task<string> GetContainerLogsAsync(string nameOrId, int lines = 100, CancellationToken cancellationToken = default)
    {
        return await ExecuteDockerCommandAsync($"logs --tail {lines} {nameOrId}", cancellationToken);
    }

    private async Task<string> ExecuteDockerCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            throw new Exception($"Docker command failed: {error}");
        }

        return output.Trim();
    }

    private List<PortMapping> ParsePorts(string portsStr)
    {
        var ports = new List<PortMapping>();
        if (string.IsNullOrWhiteSpace(portsStr))
            return ports;

        // Format: "0.0.0.0:7777->7777/tcp, :::7777->7777/tcp, 0.0.0.0:7777->7777/udp"
        var portMappings = portsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var mapping in portMappings)
        {
            var parts = mapping.Trim().Split("->");
            if (parts.Length == 2)
            {
                var publicPart = parts[0].Split(':');
                var privatePart = parts[1].Split('/');

                if (publicPart.Length >= 2 && privatePart.Length >= 2)
                {
                    if (int.TryParse(publicPart[^1], out var publicPort) &&
                        int.TryParse(privatePart[0], out var privatePort))
                    {
                        // Only add unique port mappings (skip IPv6 duplicates)
                        if (!ports.Any(p => p.PublicPort == publicPort && p.PrivatePort == privatePort && p.Type == privatePart[1]))
                        {
                            ports.Add(new PortMapping
                            {
                                PublicPort = publicPort,
                                PrivatePort = privatePort,
                                Type = privatePart[1]
                            });
                        }
                    }
                }
            }
        }

        return ports;
    }
}
