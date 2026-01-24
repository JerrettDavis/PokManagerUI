using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;
using PokManager.Web.Data.Entities;

namespace PokManager.Web.Data;

/// <summary>
/// Database-backed implementation of IConfigurationTemplateStore.
/// Handles CRUD operations for configuration templates with JSON serialization.
/// </summary>
public class ConfigurationTemplateStore : IConfigurationTemplateStore
{
    private readonly PokManagerDbContext _dbContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationTemplateStore(PokManagerDbContext dbContext)
    {
        _dbContext = dbContext;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<Result<IReadOnlyList<ConfigurationTemplateInfo>>> ListTemplatesAsync(
        string? category = null,
        int? type = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.ConfigurationTemplates.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => t.Category == category);
            }

            if (type.HasValue)
            {
                query = query.Where(t => t.Type == type.Value);
            }

            // Order by type (presets first) then by name
            query = query.OrderBy(t => t.Type).ThenBy(t => t.Name);

            var entities = await query.ToListAsync(cancellationToken);

            var templates = entities.Select(MapToInfo).ToList();

            return Result<IReadOnlyList<ConfigurationTemplateInfo>>.Success(templates);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<ConfigurationTemplateInfo>>(
                $"Failed to list templates: {ex.Message}");
        }
    }

    public async Task<Result<ConfigurationTemplateInfo>> GetTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbContext.ConfigurationTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId, cancellationToken);

            if (entity == null)
                return Result.Failure<ConfigurationTemplateInfo>($"Template '{templateId}' not found");

            var info = MapToInfo(entity);
            return Result<ConfigurationTemplateInfo>.Success(info);
        }
        catch (Exception ex)
        {
            return Result.Failure<ConfigurationTemplateInfo>(
                $"Failed to get template: {ex.Message}");
        }
    }

    public async Task<Result<string>> SaveTemplateAsync(
        ConfigurationTemplateInfo template,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = MapToEntity(template);
            entity.CreatedAt = DateTime.UtcNow;

            await _dbContext.ConfigurationTemplates.AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result<string>.Success(entity.TemplateId);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to save template: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> UpdateTemplateAsync(
        string templateId,
        ConfigurationTemplateInfo template,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbContext.ConfigurationTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId, cancellationToken);

            if (entity == null)
                return Result.Failure<Unit>($"Template '{templateId}' not found");

            // Don't allow updating preset templates
            if (entity.Type == 0)
                return Result.Failure<Unit>("Cannot update preset templates");

            // Update fields
            entity.Name = template.Name;
            entity.Description = template.Description;
            entity.Category = template.Category;
            entity.Difficulty = template.Difficulty;
            entity.MapCompatibility = string.Join(",", template.MapCompatibility);
            entity.Tags = string.Join(",", template.Tags);
            entity.ConfigurationDataJson = JsonSerializer.Serialize(template.ConfigurationData, _jsonOptions);
            entity.IncludedSettingsJson = JsonSerializer.Serialize(template.IncludedSettings, _jsonOptions);
            entity.IsPartial = template.IsPartial;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure<Unit>($"Failed to update template: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> DeleteTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbContext.ConfigurationTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId, cancellationToken);

            if (entity == null)
                return Result.Failure<Unit>($"Template '{templateId}' not found");

            // Don't allow deleting preset templates
            if (entity.Type == 0)
                return Result.Failure<Unit>("Cannot delete preset templates");

            _dbContext.ConfigurationTemplates.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure<Unit>($"Failed to delete template: {ex.Message}");
        }
    }

    public async Task<Result<Stream>> ExportTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var templateResult = await GetTemplateAsync(templateId, cancellationToken);
            if (templateResult.IsFailure)
                return Result.Failure<Stream>(templateResult.Error);

            var template = templateResult.Value;

            // Serialize template to JSON
            var json = JsonSerializer.Serialize(template, _jsonOptions);
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            stream.Position = 0;

            return Result<Stream>.Success(stream);
        }
        catch (Exception ex)
        {
            return Result.Failure<Stream>($"Failed to export template: {ex.Message}");
        }
    }

    public async Task<Result<string>> ImportTemplateAsync(
        Stream templateData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Deserialize template from JSON
            var template = await JsonSerializer.DeserializeAsync<ConfigurationTemplateInfo>(
                templateData, _jsonOptions, cancellationToken);

            if (template == null)
                return Result.Failure<string>("Invalid template data");

            // Generate new template ID and mark as user-created
            var newTemplateId = Guid.NewGuid().ToString();
            var importedTemplate = template with
            {
                TemplateId = newTemplateId,
                Type = 1, // UserCreated
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = null,
                TimesUsed = 0
            };

            // Save imported template
            var saveResult = await SaveTemplateAsync(importedTemplate, cancellationToken);
            if (saveResult.IsFailure)
                return saveResult;

            return Result<string>.Success(newTemplateId);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to import template: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> IncrementUsageCountAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbContext.ConfigurationTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId, cancellationToken);

            if (entity == null)
                return Result.Failure<Unit>($"Template '{templateId}' not found");

            entity.TimesUsed++;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure<Unit>($"Failed to increment usage count: {ex.Message}");
        }
    }

    private ConfigurationTemplateInfo MapToInfo(ConfigurationTemplate entity)
    {
        var configData = JsonSerializer.Deserialize<Dictionary<string, string>>(
            entity.ConfigurationDataJson, _jsonOptions) ?? new Dictionary<string, string>();

        var includedSettings = string.IsNullOrWhiteSpace(entity.IncludedSettingsJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(entity.IncludedSettingsJson, _jsonOptions) ?? Array.Empty<string>();

        var mapCompat = string.IsNullOrWhiteSpace(entity.MapCompatibility)
            ? Array.Empty<string>()
            : entity.MapCompatibility.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var tags = string.IsNullOrWhiteSpace(entity.Tags)
            ? Array.Empty<string>()
            : entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new ConfigurationTemplateInfo(
            entity.TemplateId,
            entity.Name,
            entity.Description,
            entity.Type,
            entity.IsPartial,
            entity.Category,
            entity.Difficulty,
            mapCompat,
            tags,
            configData,
            includedSettings,
            new DateTimeOffset(entity.CreatedAt, TimeSpan.Zero),
            entity.UpdatedAt.HasValue ? new DateTimeOffset(entity.UpdatedAt.Value, TimeSpan.Zero) : null,
            entity.Author,
            entity.TimesUsed
        );
    }

    private ConfigurationTemplate MapToEntity(ConfigurationTemplateInfo info)
    {
        return new ConfigurationTemplate
        {
            TemplateId = info.TemplateId,
            Name = info.Name,
            Description = info.Description,
            Type = info.Type,
            IsPartial = info.IsPartial,
            Category = info.Category,
            Difficulty = info.Difficulty,
            MapCompatibility = string.Join(",", info.MapCompatibility),
            Tags = string.Join(",", info.Tags),
            ConfigurationDataJson = JsonSerializer.Serialize(info.ConfigurationData, _jsonOptions),
            IncludedSettingsJson = JsonSerializer.Serialize(info.IncludedSettings, _jsonOptions),
            Author = info.Author,
            TimesUsed = info.TimesUsed,
            CreatedAt = info.CreatedAt.UtcDateTime,
            UpdatedAt = info.UpdatedAt?.UtcDateTime
        };
    }
}
