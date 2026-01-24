using System.Text.RegularExpressions;
using PokManager.Domain.Common;

namespace PokManager.Domain.ValueObjects;

public sealed class BackupId : IEquatable<BackupId>
{
    private static readonly Regex ValidationPattern = new(
        @"^([a-zA-Z0-9_-]+)_backup_(\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})$",
        RegexOptions.Compiled);

    private const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss";

    public string Value { get; }
    public string InstanceId { get; }
    public DateTime Timestamp { get; }

    private BackupId(string value, string instanceId, DateTime timestamp)
    {
        Value = value;
        InstanceId = instanceId;
        Timestamp = timestamp;
    }

    public static Result<BackupId> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<BackupId>.Failure("BackupId cannot be empty or whitespace.");
        }

        var match = ValidationPattern.Match(value);
        if (!match.Success)
        {
            return Result<BackupId>.Failure(
                "BackupId must be in the format: {instanceId}_backup_{yyyy-MM-dd_HH-mm-ss}");
        }

        var instanceId = match.Groups[1].Value;
        var timestampStr = match.Groups[2].Value;

        if (!DateTime.TryParseExact(
            timestampStr,
            TimestampFormat,
            null,
            System.Globalization.DateTimeStyles.None,
            out var timestamp))
        {
            return Result<BackupId>.Failure("BackupId timestamp format is invalid.");
        }

        return Result<BackupId>.Success(new BackupId(value, instanceId, timestamp));
    }

    public static Result<BackupId> CreateFromComponents(InstanceId instanceId, DateTime timestamp)
    {
        if (instanceId is null)
        {
            return Result<BackupId>.Failure("InstanceId cannot be null.");
        }

        var timestampStr = timestamp.ToString(TimestampFormat);
        var value = $"{instanceId.Value}_backup_{timestampStr}";

        return Result<BackupId>.Success(new BackupId(value, instanceId.Value, timestamp));
    }

    public bool Equals(BackupId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is BackupId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(BackupId? left, BackupId? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(BackupId? left, BackupId? right)
    {
        return !Equals(left, right);
    }
}
