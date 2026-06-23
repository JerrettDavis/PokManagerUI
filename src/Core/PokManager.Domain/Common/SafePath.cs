using System.Text.RegularExpressions;

namespace PokManager.Domain.Common;

/// <summary>
/// Helpers for validating externally-supplied identifiers and resolving file-system
/// paths safely. Centralizes path-traversal and command-injection defenses so that
/// untrusted input (e.g. instance ids, file names) cannot escape an expected base
/// directory or inject shell/argument metacharacters.
/// </summary>
public static partial class SafePath
{
    /// <summary>
    /// Characters allowed in identifiers that are embedded in file paths or process
    /// arguments. Deliberately excludes path separators, whitespace, quotes and
    /// shell metacharacters so the value is safe to interpolate.
    /// </summary>
    [GeneratedRegex("^[a-zA-Z0-9_-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeIdentifierRegex();

    /// <summary>
    /// Characters allowed in file-name tokens such as backup ids. Adds the dot to the
    /// identifier set (for extensions like <c>.tar.gz</c>) while still excluding path
    /// separators, whitespace, quotes and shell metacharacters.
    /// </summary>
    [GeneratedRegex(@"^[a-zA-Z0-9_.-]{1,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeFileTokenRegex();

    /// <summary>
    /// Returns <c>true</c> when <paramref name="value"/> is a safe identifier
    /// (alphanumeric, hyphen or underscore, 1-64 chars) containing no path
    /// separators or shell metacharacters.
    /// </summary>
    public static bool IsSafeIdentifier(string? value) =>
        !string.IsNullOrEmpty(value) && SafeIdentifierRegex().IsMatch(value);

    /// <summary>
    /// Validates an externally-supplied identifier (such as an instance id) and
    /// returns it unchanged, or throws if it contains anything other than
    /// alphanumeric characters, hyphens or underscores. The returned value is safe
    /// to interpolate into file paths and process arguments.
    /// </summary>
    /// <exception cref="ArgumentException">The identifier is null, empty, too long or contains disallowed characters.</exception>
    public static string ValidateIdentifier(string? value, string paramName = "identifier")
    {
        if (!IsSafeIdentifier(value))
        {
            throw new ArgumentException(
                $"Invalid identifier. Only alphanumeric characters, hyphens and underscores (max 64) are allowed.",
                paramName);
        }

        return value!;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="value"/> is a safe file-name token
    /// (alphanumeric, dot, hyphen or underscore, 1-128 chars) that contains no path
    /// separators, parent-directory references or shell metacharacters.
    /// </summary>
    public static bool IsSafeFileToken(string? value) =>
        !string.IsNullOrEmpty(value)
        && SafeFileTokenRegex().IsMatch(value)
        && !value.Contains("..", StringComparison.Ordinal);

    /// <summary>
    /// Validates an externally-supplied file-name token (such as a backup id) and
    /// returns it unchanged, or throws if it contains path separators, parent-directory
    /// references or shell metacharacters. The returned value is safe to interpolate
    /// into file paths and process arguments.
    /// </summary>
    /// <exception cref="ArgumentException">The token is null, empty, too long or contains disallowed characters.</exception>
    public static string ValidateFileToken(string? value, string paramName = "value")
    {
        if (!IsSafeFileToken(value))
        {
            throw new ArgumentException(
                "Invalid value. Only alphanumeric characters, dots, hyphens and underscores (max 128, no '..') are allowed.",
                paramName);
        }

        return value!;
    }

    /// <summary>
    /// Neutralizes carriage-return and line-feed characters (and other control
    /// characters) in a value before it is written to a log, preventing log-forging
    /// where attacker-controlled input injects forged log lines.
    /// </summary>
    public static string SanitizeLogValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            builder.Append(c is '\r' or '\n' ? '_' : char.IsControl(c) ? ' ' : c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Removes shell metacharacters from free-form text so it can be safely embedded
    /// in a double-quoted shell argument. Strips quotes, command-substitution,
    /// redirection, piping, chaining and newline characters.
    /// </summary>
    public static string SanitizeShellText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        Span<char> dangerous = ['"', '\'', '`', '$', '\\', ';', '|', '&', '<', '>', '\n', '\r', '\0'];
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (dangerous.IndexOf(c) < 0)
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Combines <paramref name="baseDirectory"/> with one or more untrusted
    /// <paramref name="segments"/>, canonicalizes the result with
    /// <see cref="Path.GetFullPath(string)"/>, and verifies the resolved path stays
    /// within <paramref name="baseDirectory"/>. Any attempt to traverse outside the
    /// base directory (e.g. via <c>..</c> or absolute paths) is rejected.
    /// </summary>
    /// <returns>The canonicalized, validated absolute path.</returns>
    /// <exception cref="ArgumentException">The resolved path escapes the base directory.</exception>
    public static string ResolveWithin(string baseDirectory, params string[] segments)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseDirectory);

        var basePath = Path.GetFullPath(baseDirectory);
        var baseWithSeparator = basePath.EndsWith(Path.DirectorySeparatorChar)
            ? basePath
            : basePath + Path.DirectorySeparatorChar;

        var combined = basePath;
        foreach (var segment in segments)
        {
            // Reject rooted segments outright; Path.Combine would discard the base.
            if (Path.IsPathRooted(segment))
            {
                throw new ArgumentException("Path segment must be relative.", nameof(segments));
            }

            combined = Path.Combine(combined, segment);
        }

        var resolved = Path.GetFullPath(combined);

        if (!resolved.Equals(basePath, StringComparison.Ordinal) &&
            !resolved.StartsWith(baseWithSeparator, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Resolved path escapes the permitted base directory.", nameof(segments));
        }

        return resolved;
    }
}
