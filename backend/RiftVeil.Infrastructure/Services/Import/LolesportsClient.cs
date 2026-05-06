using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// HTTP client for Riot's persisted lolesports GraphQL gateway (schedule, events, VOD metadata).
/// </summary>
public class LolesportsClient(HttpClient httpClient, IOptions<LolesportsClientOptions> options, ILogger<LolesportsClient> logger)
{
    private const string BaseUrl = "https://esports-api.lolesports.com/persisted/gw";

    private readonly LolesportsClientOptions _options = options.Value;

    /// <summary>
    /// Calls a persisted gateway endpoint (e.g. <c>getCompletedEvents</c>) with optional query parameters.
    /// Retries on failure with backoff. Throws if the API key is not configured.
    /// </summary>
    /// <param name="endpoint">Gateway operation name.</param>
    /// <param name="parameters">Optional query parameters appended to the request.</param>
    /// <returns>Parsed JSON response body.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="LolesportsClientOptions.ApiKey"/> is missing.</exception>
    public async Task<JsonDocument> CallAsync(string endpoint, Dictionary<string, string>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                $"Lolesports API key is not configured. Set '{LolesportsClientOptions.SectionName}:ApiKey' in appsettings, user secrets, or environment variables.");
        }

        var url = $"{BaseUrl}/{endpoint}?hl=en-US";
        if (parameters is { Count: > 0 })
        {
            url += "&" + string.Join("&", parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        }

        logger.LogDebug("[Lolesports] Fetching {Endpoint}", endpoint);

        for (int attempt = 0; attempt < _options.MaxAttempts; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(attempt * _options.RetryDelayMilliseconds);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", _options.ApiKey);

            var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var doc = JsonDocument.Parse(body);

                // Riot returns 200 with {"message":"Forbidden"} instead of 403
                if (doc.RootElement.TryGetProperty("message", out var msg) &&
                    msg.GetString() == "Forbidden")
                {
                    logger.LogWarning("[Lolesports] Forbidden for {Endpoint} (API key may be invalid)", endpoint);
                    throw new Exception($"Lolesports API returned Forbidden for {endpoint}");
                }

                logger.LogDebug("[Lolesports] OK: {Endpoint}", endpoint);
                return doc;
            }

            logger.LogWarning("[Lolesports] Attempt {Attempt} failed for {Endpoint}: {StatusCode}",
                attempt + 1, endpoint, response.StatusCode);
        }

        throw new Exception($"Lolesports API failed for {endpoint} after {_options.MaxAttempts} attempts");
    }
}
