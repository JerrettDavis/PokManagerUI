using System.ComponentModel.DataAnnotations;
using PokManager.Domain.Enumerations;

namespace PokManager.Web.Models;

/// <summary>
/// View model for creating a new backup.
/// </summary>
public class CreateBackupViewModel
{
    [Required(ErrorMessage = "Instance is required")]
    public string InstanceId { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Compression format is required")]
    public CompressionFormat CompressionFormat { get; set; } = CompressionFormat.Gzip;

    public bool IncludeConfiguration { get; set; } = true;

    public bool IncludeLogs { get; set; } = false;
}
