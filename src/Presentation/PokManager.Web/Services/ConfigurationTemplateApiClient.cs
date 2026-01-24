using System.Net.Http.Json;
using System.Text.Json;
using PokManager.Application.Models;
using PokManager.Application.UseCases.ConfigurationTemplates.ApplyTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.ListTemplates;
using PokManager.Application.UseCases.ConfigurationTemplates.PreviewTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.SaveTemplate;

namespace PokManager.Web.Services;

/// <summary>
/// HTTP client for interacting with configuration template API endpoints.
/// </summary>
public class ConfigurationTemplateApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationTemplateApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<List<TemplateSummaryDto>> GetTemplatesAsync(
        string? category = null,
        int? type = null,
        string? mapFilter = null)
    {
        var query = BuildQueryString(category, type, mapFilter);
        var response = await _httpClient.GetFromJsonAsync<ListTemplatesResponse>(
            $"/api/configuration-templates{query}", _jsonOptions);

        return response?.Templates.ToList() ?? new List<TemplateSummaryDto>();
    }

    public async Task<ConfigurationTemplateInfo?> GetTemplateAsync(string templateId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ConfigurationTemplateInfo>(
                $"/api/configuration-templates/{templateId}", _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<SaveTemplateResponse?> CreateTemplateAsync(SaveTemplateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/configuration-templates", request, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SaveTemplateResponse>(_jsonOptions);
    }

    public async Task DeleteTemplateAsync(string templateId)
    {
        var response = await _httpClient.DeleteAsync($"/api/configuration-templates/{templateId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<ApplyTemplateResponse?> ApplyTemplateAsync(
        string templateId,
        string instanceId,
        bool createBackup = true,
        bool restartIfNeeded = true)
    {
        var requestBody = new
        {
            InstanceId = instanceId,
            CreateBackup = createBackup,
            RestartIfNeeded = restartIfNeeded
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/configuration-templates/{templateId}/apply",
            requestBody,
            _jsonOptions);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApplyTemplateResponse>(_jsonOptions);
    }

    public async Task<PreviewTemplateResponse?> PreviewTemplateAsync(
        string templateId,
        string instanceId)
    {
        var requestBody = new { InstanceId = instanceId };

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/configuration-templates/{templateId}/preview",
            requestBody,
            _jsonOptions);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PreviewTemplateResponse>(_jsonOptions);
    }

    public async Task<Stream> ExportTemplateAsync(string templateId)
    {
        var response = await _httpClient.GetAsync($"/api/configuration-templates/{templateId}/export");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    private string BuildQueryString(string? category, int? type, string? mapFilter)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(category))
            queryParams.Add($"category={Uri.EscapeDataString(category)}");

        if (type.HasValue)
            queryParams.Add($"type={type.Value}");

        if (!string.IsNullOrWhiteSpace(mapFilter))
            queryParams.Add($"mapFilter={Uri.EscapeDataString(mapFilter)}");

        return queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
    }
}
