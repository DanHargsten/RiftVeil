using System.Text.Json;
using Microsoft.Extensions.Options;

namespace RiftVeil.Infrastructure.Services.Import;

public class LeaguepediaClient(HttpClient httpClient, IOptions<LeaguepediaClientOptions> options)
{
    private const string BaseUrl = "https://lol.fandom.com/api.php";

    private readonly LeaguepediaClientOptions _options = options.Value;

    // Enforce sequential requests — Fandom blocks bursts
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

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
        int offset = 0)
    {
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

                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(responseStream);

                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    var code = error.GetProperty("code").GetString();
                    var info = error.TryGetProperty("info", out var infoProp) ? infoProp.GetString() : "unknown";

                    if (code == "ratelimited")
                    {
                        rateLimitRetries++;
                        lastFailure = LastFailureKind.RateLimited;
                        Console.WriteLine($"  Rate limited by API (attempt {attempt + 1}/{maxAttempts})");
                        continue;
                    }

                    if (IsTransientApiError(code))
                    {
                        transientRetries++;
                        lastFailure = LastFailureKind.Transient;
                        Console.WriteLine($"  Transient API error [{code}] (attempt {attempt + 1}/{maxAttempts})");
                        continue;
                    }

                    Console.WriteLine($"  API Error [{code}]: {info}");
                    return [];
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

                return results;
            }

            Console.WriteLine("  Max retries exceeded");
            return [];
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
}
