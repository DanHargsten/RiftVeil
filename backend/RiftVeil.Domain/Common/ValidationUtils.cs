namespace RiftVeil.Domain.Common;

/// <summary>
/// Provides utility methods for validating domain entities.
/// </summary>
public static class ValidationUtils
{
    /// <summary>
    /// Validates a name string.
    /// </summary>
    /// <param name="value">The name string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <returns>The trimmed name string.</returns>
    public static string ValidateName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name cannot be empty.", paramName);

        if (value.Length > 100)
            throw new ArgumentException("Name cannot exceed 100 characters.", paramName);

        return value.Trim();
    }


    /// <summary>
    /// Validates a short name string.
    /// </summary>
    /// <param name="value">The short name string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <returns>The trimmed and upper-cased short name string.</returns>
    public static string ValidateShortName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Short name cannot be empty.", paramName);

        var trimmed = value.Trim();

        if (trimmed.Length > 20)
            throw new ArgumentException("Short name cannot exceed 20 characters.", paramName);

        if (trimmed.Any(char.IsWhiteSpace))
            throw new ArgumentException("Short name cannon contain spaces", paramName);

        return trimmed.ToUpperInvariant();
    }


    /// <summary>
    /// Normalizes an optional string.
    /// </summary>
    /// <param name="value">The optional string to normalize.</param>
    /// <returns>The trimmed string or null if the input is null or whitespace.</returns>
    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }


    /// <summary>
    /// Ensures that a DateTimeOffset is in UTC.
    /// </summary>
    /// <param name="value">The DateTimeOffset to ensure is in UTC.</param>
    /// <returns>The DateTimeOffset in UTC.</returns>
    public static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            return value.ToUniversalTime();
        }

        return value;
    }


    /// <summary>
    /// Ensures that a DateTime is in UTC.
    /// </summary>
    /// <param name="value">The DateTime to ensure is in UTC.</param>
    /// <returns>The DateTime in UTC.</returns>
    public static DateTimeOffset EnsureUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            // Assume UTC if the kind is unspecified
            return new DateTimeOffset(value, TimeSpan.Zero);
        }

        return new DateTimeOffset(value.ToUniversalTime());
    }
}
