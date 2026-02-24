namespace RiftVeil.Domain.Common;

/// <summary>
/// Centralizes validation rules so entities stay consistent.
/// </summary>
public static class ValidationUtils
{
    /// <summary>
    /// Keeps human-facing names within shared constraints.
    /// </summary>
    public static string ValidateName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Name cannot be empty.", paramName);
        }

        if (value.Length > 100)
        {
            throw new ArgumentException("Name cannot exceed 100 characters.", paramName);
        }

        return value.Trim();
    }


    /// <summary>
    /// Normalizes short codes for indexing and comparisons.
    /// </summary>
    public static string ValidateShortName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Short name cannot be empty.", paramName);
        }

        var trimmed = value.Trim();

        if (trimmed.Length > 20)
        {
            throw new ArgumentException("Short name cannot exceed 20 characters.", paramName);
        }

        if (trimmed.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Short name cannot contain spaces", paramName);
        }

        return trimmed.ToUpperInvariant();
    }


    /// <summary>
    /// Avoids persisting empty strings for optional fields.
    /// </summary>
    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }


    /// <summary>
    /// Standardizes timestamps to UTC for storage.
    /// </summary>
    public static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            return value.ToUniversalTime();
        }

        return value;
    }


    /// <summary>
    /// Treats unspecified kinds as UTC to avoid local-time drift.
    /// </summary>
    public static DateTimeOffset EnsureUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            // Treat unspecified as UTC to avoid implicit local conversion.
            return new DateTimeOffset(value, TimeSpan.Zero);
        }

        return new DateTimeOffset(value.ToUniversalTime());
    }
}
