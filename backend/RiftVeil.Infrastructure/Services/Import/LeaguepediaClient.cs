using System.Text.Json;
using Microsoft.Extensions.Options;

namespace RiftVeil.Infrastructure.Services.Import;

public class LeaguepediaClient(HttpClient httpClient, IOptions<LeaguepediaClientOptions> options)
{
    private const string BaseUrl = "https://lol.fandom.com/api.php";

    private readonly LeaguepediaClientOptions _options = options.Value;

    // Enforce sequential requests — Fandom blocks bursts
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    // One-time bot login per process. The shared CookieContainer keeps the session afterwards.
    private static readonly SemaphoreSlim LoginLock = new(1, 1);
    private static bool _loginAttempted;
    private static bool _loginSucceeded;

    private enum LastFailureKind
    {
        None,
        RateLimited,
        Transient
    }

    public async Task<List<JsonElement>> QueryAsync(
        string tables,
        string fields,
        string? where = null,
        string? orderBy = null,
        int limit = 50,
        int offset = 0) =>
        (await QueryWithOutcomeAsync(tables, fields, where, orderBy, limit, offset)).Rows;

    /// <summary>
    /// Same as <see cref="QueryAsync"/> but distinguishes Cargo/MediaWiki failures (after retries)
    /// from a successful response (including legitimately empty <c>cargoquery</c> arrays).
    /// </summary>
    public async Task<(bool Succeeded, List<JsonElement> Rows)> QueryWithOutcomeAsync(
        string tables,
        string fields,
        string? where = null,
        string? orderBy = null,
        int limit = 50,
        int offset = 0)
    {
        await EnsureLoggedInAsync();

        var query = $"?action=cargoquery" +
                    $"&tables={Uri.EscapeDataString(tables)}" +
                    $"&fields={Uri.EscapeDataString(fields)}" +
                    $"&format=json" +
                    $"&limit={limit}";

        if (offset > 0)
            query += $"&offset={offset}";

        if (!string.IsNullOrEmpty(where))
            query += $"&where={Uri.EscapeDataString(where)}";

        if (!string.IsNullOrEmpty(orderBy))
            query += $"&order_by={Uri.EscapeDataString(orderBy)}";

        var url = BaseUrl + query;
        Console.WriteLine($"Fetching: {url}");

        await Semaphore.WaitAsync();
        try
        {
            var rateLimitRetries = 0;
            var transientRetries = 0;
            var lastFailure = LastFailureKind.None;
            var maxAttempts = Math.Max(1, _options.RateLimitMaxAttempts);

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    var delayMs = lastFailure switch
                    {
                        LastFailureKind.RateLimited => ComputeRateLimitDelayMs(rateLimitRetries),
                        LastFailureKind.Transient => ComputeTransientDelayMs(transientRetries),
                        _ => 0
                    };

                    if (delayMs > 0)
                    {
                        var label = lastFailure switch
                        {
                            LastFailureKind.RateLimited => "Rate limited",
                            LastFailureKind.Transient => "Transient API error",
                            _ => "Retry"
                        };
                        Console.WriteLine(
                            $"  {label} (attempt {attempt + 1}/{maxAttempts}), waiting {delayMs / 1000.0:0.#}s...");
                        await Task.Delay(delayMs);
                    }
                }

                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                Console.WriteLine($"  Status: {response.StatusCode}");
                response.EnsureSuccessStatusCode();

                // Buffer the body so we can both parse it and log it on diagnostics paths.
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    var code = error.GetProperty("code").GetString();
                    var info = error.TryGetProperty("info", out var infoProp) ? infoProp.GetString() : "unknown";

                    if (code == "ratelimited")
                    {
                        rateLimitRetries++;
                        lastFailure = LastFailureKind.RateLimited;
                        Console.WriteLine($"  Rate limited by API (attempt {attempt + 1}/{maxAttempts})");

                        // Server-side hints help us tune backoff. Log only once per query
                        // to avoid spamming, even when the loop retries.
                        if (rateLimitRetries == 1)
                            LogRateLimitDiagnostics(response);

                        continue;
                    }

                    if (IsTransientApiError(code))
                    {
                        transientRetries++;
                        lastFailure = LastFailureKind.Transient;
                        Console.WriteLine($"  Transient API error [{code}] (attempt {attempt + 1}/{maxAttempts})");

                        // Log a short body snippet on the first transient error per query — this
                        // surfaces the actual MediaWiki/Cargo failure (e.g. broken virtual field),
                        // which the JSON `error` object alone never reveals.
                        if (transientRetries == 1)
                            LogTransientErrorBody(body);

                        var transientCap = Math.Max(1, _options.MaxTransientRetriesPerQuery);
                        if (transientRetries >= transientCap)
                        {
                            Console.WriteLine(
                                $"  Giving up after {transientRetries} transient errors for the same query (cap={transientCap}). The query likely triggers a server-side bug.");
                            return (false, []);
                        }

                        continue;
                    }

                    Console.WriteLine($"  API Error [{code}]: {info}");
                    return (false, []);
                }

                var results = doc.RootElement
                    .GetProperty("cargoquery")
                    .EnumerateArray()
                    .Select(item => item.GetProperty("title").Clone())
                    .ToList();

                Console.WriteLine($"  Got {results.Count} rows");

                var postSuccess = Math.Max(0, _options.PostSuccessDelayMilliseconds);
                if (postSuccess > 0)
                    await Task.Delay(postSuccess);

                return (true, results);
            }

            Console.WriteLine("  Max retries exceeded");
            return (false, []);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static bool IsTransientApiError(string? code) =>
        code == "internal_api_error_MWException"
        || (code != null && code.StartsWith("internal_api_error", StringComparison.Ordinal));

    /// <summary>
    /// After the first rate-limit, apply a long cooldown; then exponential backoff (capped).
    /// <paramref name="rateLimitRetries"/> is the number of rate-limit responses already seen.
    /// </summary>
    private int ComputeRateLimitDelayMs(int rateLimitRetries)
    {
        if (rateLimitRetries <= 0)
            return 0;
        
        int baseWait = _options.RateLimitExtendedCooldownMilliseconds;
        
        if (rateLimitRetries == 1) return baseWait;

        var exp = rateLimitRetries - 2;
        var backoff = (long)_options.RateLimitBackoffBaseMilliseconds << exp;
    
        return (int)Math.Min(baseWait + backoff, _options.RateLimitBackoffMaxMilliseconds);
    }

    /// <summary>
    /// Exponential backoff for transient Cargo/MediaWiki errors (no extended cooldown).
    /// <paramref name="transientRetries"/> is the count of transient errors already seen.
    /// </summary>
    private int ComputeTransientDelayMs(int transientRetries)
    {
        if (transientRetries <= 0)
            return 0;

        var baseMs = Math.Max(1, _options.TransientApiErrorBackoffBaseMilliseconds);
        var cap = _options.TransientApiErrorBackoffMaxMilliseconds;
        var exp = transientRetries - 1;
        if (exp < 0)
            exp = 0;

        var shifted = (long)baseMs << exp;
        return (int)Math.Min(shifted, cap);
    }

    // =====================================================================
    //  BOT LOGIN  (one-time per process; cookie session is shared)
    // =====================================================================

    /// <summary>
    /// Performs MediaWiki <c>action=login</c> using bot credentials from options on first use.
    /// Subsequent calls are no-ops. Falls back to anonymous (current behaviour) when credentials are missing.
    /// </summary>
    private async Task EnsureLoggedInAsync()
    {
        if (_loginAttempted)
            return;

        if (string.IsNullOrWhiteSpace(_options.BotUsername) || string.IsNullOrWhiteSpace(_options.BotPassword))
        {
            _loginAttempted = true;
            Console.WriteLine("  Leaguepedia: no bot credentials configured — running anonymously (lower rate limits).");
            return;
        }

        await LoginLock.WaitAsync();
        try
        {
            if (_loginAttempted)
                return;

            try
            {
                var token = await FetchLoginTokenAsync();
                if (token == null)
                {
                    Console.WriteLine("  Leaguepedia bot login: failed to fetch login token; continuing anonymously.");
                    return;
                }

                _loginSucceeded = await PostLoginAsync(_options.BotUsername!, _options.BotPassword!, token);
                Console.WriteLine(_loginSucceeded
                    ? $"  Leaguepedia bot login: success as '{_options.BotUsername}'."
                    : "  Leaguepedia bot login: failed; continuing anonymously.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Leaguepedia bot login: exception ({ex.GetType().Name}: {ex.Message}); continuing anonymously.");
            }
            finally
            {
                _loginAttempted = true;
            }
        }
        finally
        {
            LoginLock.Release();
        }
    }

    private async Task<string?> FetchLoginTokenAsync()
    {
        const string tokenUrl = BaseUrl + "?action=query&meta=tokens&type=login&format=json";
        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, tokenUrl));
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement
            .GetProperty("query").GetProperty("tokens").GetProperty("logintoken")
            .GetString();
    }

    private async Task<bool> PostLoginAsync(string botUsername, string botPassword, string loginToken)
    {
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["action"]     = "login",
            ["lgname"]     = botUsername,
            ["lgpassword"] = botPassword,
            ["lgtoken"]    = loginToken,
            ["format"]     = "json"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl) { Content = body };
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        if (!doc.RootElement.TryGetProperty("login", out var login)
            || !login.TryGetProperty("result", out var resultProp))
        {
            return false;
        }

        var result = resultProp.GetString();
        if (string.Equals(result, "Success", StringComparison.Ordinal))
            return true;

        var reason = login.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : null;
        Console.WriteLine($"  Leaguepedia bot login: result='{result}'{(reason != null ? $", reason='{reason}'" : "")}");
        return false;
    }

    // =====================================================================
    //  RATE-LIMIT DIAGNOSTICS
    // =====================================================================

    /// <summary>
    /// Logs server-side rate-limit hints (Retry-After, X-RateLimit-*, MediaWiki-API-Error) to make
    /// cooldown lengths visible. Called once per query when ratelimited is first observed.
    /// </summary>
    private static void LogRateLimitDiagnostics(HttpResponseMessage response)
    {
        var headersOfInterest = new[]
        {
            "Retry-After",
            "X-RateLimit-Limit",
            "X-RateLimit-Remaining",
            "X-RateLimit-Reset",
            "MediaWiki-API-Error",
            "X-Database-Lag"
        };

        var found = new List<string>();
        foreach (var name in headersOfInterest)
        {
            if (response.Headers.TryGetValues(name, out var values))
                found.Add($"{name}={string.Join(",", values)}");
            else if (response.Content.Headers.TryGetValues(name, out var contentValues))
                found.Add($"{name}={string.Join(",", contentValues)}");
        }

        Console.WriteLine(found.Count > 0
            ? $"  Rate-limit headers: {string.Join("; ", found)}"
            : "  Rate-limit headers: (none of Retry-After / X-RateLimit-* / MediaWiki-API-Error present)");
    }

    /// <summary>
    /// Logs a truncated snippet of the response body when MediaWiki returns an
    /// <c>internal_api_error_*</c> for the first time in a query. The body usually contains
    /// the underlying PHP exception or Cargo error message that's hidden from the JSON envelope.
    /// </summary>
    private static void LogTransientErrorBody(string body)
    {
        const int maxChars = 500;
        var snippet = body.Length <= maxChars
            ? body
            : body.Substring(0, maxChars) + "…";
        Console.WriteLine($"  Transient error body (truncated): {snippet}");
    }
}
