using System.Net.Http.Json;

namespace PokManager.Web.Services;

public class ServerService
{
    private readonly HttpClient _httpClient;

    public ServerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ContainerDto>?> GetServersAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ContainerDto>>("/api/servers");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching servers: {ex.Message}");
            return null;
        }
    }

    public async Task<ContainerDto?> GetServerAsync(string nameOrId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ContainerDto>($"/api/servers/{nameOrId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> StartServerAsync(string nameOrId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/servers/{nameOrId}/start", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StopServerAsync(string nameOrId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/servers/{nameOrId}/stop", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RestartServerAsync(string nameOrId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/servers/{nameOrId}/restart", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetServerLogsAsync(string nameOrId, int lines = 100)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<LogResponse>($"/api/servers/{nameOrId}/logs?lines={lines}");
            return response?.Logs;
        }
        catch
        {
            return null;
        }
    }
}

public class ContainerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public List<PortMappingDto> Ports { get; set; } = new();
    public Dictionary<string, string> Labels { get; set; } = new();
}

public class PortMappingDto
{
    public int PrivatePort { get; set; }
    public int PublicPort { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class LogResponse
{
    public string Logs { get; set; } = string.Empty;
}
