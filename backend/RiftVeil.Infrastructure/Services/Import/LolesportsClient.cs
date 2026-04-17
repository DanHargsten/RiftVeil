using System.Text.Json;

using Microsoft.Extensions.Options;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// HTTP client for Riot's persisted lolesports GraphQL gateway (schedule, events, VOD metadata).
/// </summary>
public class LolesportsClient(HttpClient httpClient, IOptions<LolesportsClientOptions> options)
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

        Console.WriteLine($"[Lolesports] Fetching {endpoint}...");

        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(attempt * 5000);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", _options.ApiKey);

            Console.WriteLine($"[Lolesports] URL: {request.RequestUri}");
            Console.WriteLine($"[Lolesports] Headers: {string.Join(", ", request.Headers.Select(header => $"{header.Key}={string.Join(",", header.Value)}"))}");

            var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Lolesports] Response ({response.StatusCode}): {body[..Math.Min(200, body.Length)]}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Lolesports] OK: {endpoint}");
                var doc = JsonDocument.Parse(body);

                // Riot returns 200 with {"message":"Forbidden"} instead of 403
                if (doc.RootElement.TryGetProperty("message", out var msg) &&
                    msg.GetString() == "Forbidden")
                {
                    Console.WriteLine($"[Lolesports] Forbidden for {endpoint} (API key may be invalid)");
                    throw new Exception($"Lolesports API returned Forbidden for {endpoint}");
                }

                return doc;
            }

            Console.WriteLine($"[Lolesports] Attempt {attempt + 1} failed: {response.StatusCode}");
        }

        throw new Exception($"Lolesports API failed for {endpoint}");
    }
}
