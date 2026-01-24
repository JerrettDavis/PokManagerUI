using System.Diagnostics;
using System.Text.Json;
using PokManager.Infrastructure.Docker.Models;

namespace PokManager.Infrastructure.Docker.Services;

public class SshDockerService : IDockerService
{
    private readonly string _sshHost;
    private readonly string _sshUser;

    public SshDockerService(string sshHost, string sshUser)
    {
        _sshHost = sshHost ?? throw new ArgumentNullException(nameof(sshHost));
        _sshUser = sshUser ?? throw new ArgumentNullException(nameof(sshUser));
    }

    public async Task<List<ContainerInfo>> ListContainersAsync(CancellationToken cancellationToken = default)
    {
        var output = await ExecuteSshCommandAsync("docker ps -a --format json", cancellationToken);

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
                    Created = DateTimeOffset.FromUnixTimeSeconds(
                        long.Parse(root.GetProperty("CreatedAt").GetString()?.Split(' ')[0] ?? "0")
                    ).DateTime
                };

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
        var output = await ExecuteSshCommandAsync($"docker start {nameOrId}", cancellationToken);
        return !string.IsNullOrWhiteSpace(output);
    }

    public async Task<bool> StopContainerAsync(string nameOrId, CancellationToken cancellationToken = default)
    {
        var output = await ExecuteSshCommandAsync($"docker stop {nameOrId}", cancellationToken);
        return !string.IsNullOrWhiteSpace(output);
    }

    public async Task<bool> RestartContainerAsync(string nameOrId, CancellationToken cancellationToken = default)
    {
        var output = await ExecuteSshCommandAsync($"docker restart {nameOrId}", cancellationToken);
        return !string.IsNullOrWhiteSpace(output);
    }

    public async Task<string> GetContainerLogsAsync(string nameOrId, int lines = 100, CancellationToken cancellationToken = default)
    {
        return await ExecuteSshCommandAsync($"docker logs --tail {lines} {nameOrId}", cancellationToken);
    }

    private async Task<string> ExecuteSshCommandAsync(string command, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ssh",
            Arguments = $"-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR {_sshUser}@{_sshHost} \"{command}\"",
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
            throw new Exception($"SSH command failed: {error}");
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
