namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Configuration for <see cref="LolesportsClient"/> (bound from the <c>Lolesports</c> configuration section).
/// </summary>
public class LolesportsClientOptions
{
    public const string SectionName = "Lolesports";

    /// <summary>
    /// Value for the <c>x-api-key</c> header. Use user secrets or environment variables in production.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Maximum number of attempts per request (initial try + retries).
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Base delay in milliseconds between retries (multiplied by attempt number).
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 5_000;
}
