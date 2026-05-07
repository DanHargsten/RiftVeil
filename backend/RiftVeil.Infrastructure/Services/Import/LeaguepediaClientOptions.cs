namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Throttling and retry settings for <see cref="LeaguepediaClient"/> and related import orchestration.
/// Bound from the <c>Leaguepedia</c> configuration section.
/// </summary>
public class LeaguepediaClientOptions
{
    public const string SectionName = "Leaguepedia";

    /// <summary>
    /// Pause after each successful Cargo response before releasing the semaphore (reduces burst load).
    /// </summary>
    public int PostSuccessDelayMilliseconds { get; set; } = 2_000;

    /// <summary>
    /// Pause between player / team / draft import phases in <see cref="GameDetailImportService"/>.
    /// </summary>
    public int DelayBetweenGameDetailImportPhasesMilliseconds { get; set; } = 5_000;

    /// <summary>
    /// Pause after each tournament when importing game details for all ongoing tournaments.
    /// </summary>
    public int DelayBetweenOngoingTournamentsMilliseconds { get; set; } = 30_000;

    /// <summary>
    /// Page size for Cargo <c>limit</c> when paginating game-detail imports.
    /// </summary>
    public int CargoPageSize { get; set; } = 100;

    /// <summary>
    /// Maximum HTTP attempts per <see cref="LeaguepediaClient.QueryAsync"/> (initial try + retries after rate limit or transient errors).
    /// </summary>
    public int RateLimitMaxAttempts { get; set; } = 12;

    /// <summary>
    /// One-time long wait after the first <c>ratelimited</c> response in a query (before further retries).
    /// </summary>
    public int RateLimitExtendedCooldownMilliseconds { get; set; } = 90_000;

    /// <summary>
    /// Base for exponential backoff (ms) after subsequent rate limits (doubled each time, capped).
    /// </summary>
    public int RateLimitBackoffBaseMilliseconds { get; set; } = 2_000;

    /// <summary>
    /// Upper cap (ms) for exponential backoff between rate-limit retries.
    /// </summary>
    public int RateLimitBackoffMaxMilliseconds { get; set; } = 60_000;

    /// <summary>
    /// Base (ms) for exponential backoff after <c>internal_api_error_*</c> responses (e.g. MWException). No extended cooldown.
    /// </summary>
    public int TransientApiErrorBackoffBaseMilliseconds { get; set; } = 5_000;

    /// <summary>
    /// Upper cap (ms) for transient API error backoff.
    /// </summary>
    public int TransientApiErrorBackoffMaxMilliseconds { get; set; } = 60_000;

    /// <summary>
    /// Maximum consecutive <c>internal_api_error_*</c> responses for the same query before giving up
    /// (independent of <see cref="RateLimitMaxAttempts"/>). Server-side crashes for a specific query
    /// rarely self-heal across short retries, so a low cap saves wall-clock time.
    /// </summary>
    public int MaxTransientRetriesPerQuery { get; set; } = 3;

    /// <summary>
    /// MediaWiki bot login name (e.g. <c>UserName@BotName</c>) for authenticated Cargo access.
    /// Empty/null = anonymous (much stricter rate limits).
    /// Stored via user secrets, never in <c>appsettings.json</c>.
    /// </summary>
    public string? BotUsername { get; set; }

    /// <summary>
    /// MediaWiki bot password generated via <c>Special:BotPasswords</c>.
    /// Stored via user secrets, never in <c>appsettings.json</c>.
    /// </summary>
    public string? BotPassword { get; set; }

    /// <summary>
    /// Contact email advertised in the Leaguepedia <c>User-Agent</c> header. Fandom asks bots to
    /// expose a way to reach the operator. Configurable so we don't hard-code a personal address
    /// in <c>Program.cs</c>; provide via <c>appsettings</c> or environment variable.
    /// </summary>
    public string? ContactEmail { get; set; }
}
