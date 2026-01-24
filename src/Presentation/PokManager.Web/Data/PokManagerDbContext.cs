using Microsoft.EntityFrameworkCore;
using PokManager.Web.Data.Entities;

namespace PokManager.Web.Data;

/// <summary>
/// Database context for PokManager persistent data.
/// </summary>
public class PokManagerDbContext : DbContext
{
    public PokManagerDbContext(DbContextOptions<PokManagerDbContext> options)
        : base(options)
    {
    }

    public DbSet<InstanceSnapshot> InstanceSnapshots { get; set; }
    public DbSet<PlayerSession> PlayerSessions { get; set; }
    public DbSet<TelemetrySnapshot> TelemetrySnapshots { get; set; }
    public DbSet<LogEntry> LogEntries { get; set; }
    public DbSet<ConfigurationTemplate> ConfigurationTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // InstanceSnapshot configuration
        modelBuilder.Entity<InstanceSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.InstanceId, e.Timestamp });
            entity.Property(e => e.InstanceId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Health).IsRequired().HasMaxLength(50);
        });

        // PlayerSession configuration
        modelBuilder.Entity<PlayerSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.InstanceId, e.SteamId, e.JoinedAt });
            entity.Property(e => e.InstanceId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PlayerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SteamId).IsRequired().HasMaxLength(100);
        });

        // TelemetrySnapshot configuration
        modelBuilder.Entity<TelemetrySnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.InstanceId, e.Timestamp });
            entity.Property(e => e.InstanceId).IsRequired().HasMaxLength(200);
        });

        // LogEntry configuration
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.InstanceId, e.Timestamp });
            entity.Property(e => e.InstanceId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Level).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Message).IsRequired();
        });

        // ConfigurationTemplate configuration
        modelBuilder.Entity<ConfigurationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TemplateId).IsUnique();
            entity.HasIndex(e => new { e.Type, e.Category });
            entity.Property(e => e.TemplateId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Difficulty).HasMaxLength(50);
            entity.Property(e => e.MapCompatibility).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Author).HasMaxLength(100);
            entity.Property(e => e.ConfigurationDataJson).IsRequired();
            entity.Property(e => e.IncludedSettingsJson).HasMaxLength(2000);
        });
    }
}
