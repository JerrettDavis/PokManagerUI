using PokManager.Application.Models;
using PokManager.Domain.Common;

namespace PokManager.Application.Ports;

/// <summary>
/// Interface for configuration template storage operations.
/// Provides access to template files and metadata, supporting retrieval, creation, and management operations.
/// </summary>
public interface IConfigurationTemplateStore
{
    /// <summary>
    /// Lists all configuration templates, optionally filtered by category and type.
    /// </summary>
    /// <param name="category">Optional category filter (e.g., "PvP", "PvE"). Null returns all categories.</param>
    /// <param name="type">Optional type filter (0=Preset, 1=UserCreated). Null returns all types.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing a read-only list of configuration template information.</returns>
    Task<Result<IReadOnlyList<ConfigurationTemplateInfo>>> ListTemplatesAsync(
        string? category = null,
        int? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific configuration template by its template ID.
    /// </summary>
    /// <param name="templateId">The unique identifier of the template.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the template information.</returns>
    Task<Result<ConfigurationTemplateInfo>> GetTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new configuration template to the store.
    /// </summary>
    /// <param name="template">The template information to save.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the new template's ID.</returns>
    Task<Result<string>> SaveTemplateAsync(
        ConfigurationTemplateInfo template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing configuration template.
    /// </summary>
    /// <param name="templateId">The unique identifier of the template to update.</param>
    /// <param name="template">The updated template information.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> UpdateTemplateAsync(
        string templateId,
        ConfigurationTemplateInfo template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a configuration template.
    /// Only user-created templates (Type=1) can be deleted; preset templates (Type=0) cannot.
    /// </summary>
    /// <param name="templateId">The unique identifier of the template to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> DeleteTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a configuration template as a JSON stream for download or backup.
    /// </summary>
    /// <param name="templateId">The unique identifier of the template to export.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing a stream of the template data in JSON format.</returns>
    Task<Result<Stream>> ExportTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a configuration template from a JSON stream.
    /// </summary>
    /// <param name="templateData">Stream containing the template data in JSON format.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the imported template's ID.</returns>
    Task<Result<string>> ImportTemplateAsync(
        Stream templateData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the usage count for a template when it's applied to an instance.
    /// </summary>
    /// <param name="templateId">The unique identifier of the template.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> IncrementUsageCountAsync(
        string templateId,
        CancellationToken cancellationToken = default);
}
