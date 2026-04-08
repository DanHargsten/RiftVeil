using System.Text.Json;

namespace RiftVeil.Infrastructure.Services.Import;

public class LolesportsClient(HttpClient httpClient)
{
    private const string BaseUrl = "https://esports-api.lolesports.com/persisted/gw";
    private const string ApiKey = "0TvQnueqKa5mxJntVWt0w4LpLfEkrV1Ta8rQBb9Z";

    public async Task<JsonDocument> CallAsync(string endpoint, Dictionary<string, string>? parameters = null)
    {
        var url = $"{BaseUrl}/{endpoint}?hl=en-US";
        if (parameters is { Count: > 0 })
        {
            url += "&" + string.Join("&", parameters.Select(p =>
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        }

        Console.WriteLine($"[Lolesports] Fetching {endpoint}...");

        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(attempt * 5000);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", ApiKey);


            Console.WriteLine($"[Lolesports] URL: {request.RequestUri}");
            Console.WriteLine($"[Lolesports] Headers: {string.Join(", ", request.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");


            var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Lolesports] Response ({response.StatusCode}): {body[..Math.Min(200, body.Length)]}");


            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Lolesports] ✓ {endpoint} OK");
                var doc = JsonDocument.Parse(body);

                // Riot returns 200 with {"message":"Forbidden"} instead of 403
                if (doc.RootElement.TryGetProperty("message", out var msg) &&
                    msg.GetString() == "Forbidden")
                {
                    Console.WriteLine($"[Lolesports] ✗ {endpoint} returned Forbidden (API key may be invalid)");
                    throw new Exception($"Lolesports API returned Forbidden for {endpoint}");
                }

                return doc;
            }

            Console.WriteLine($"[Lolesports] Attempt {attempt + 1} failed: {response.StatusCode}");
        }

        throw new Exception($"Lolesports API failed for {endpoint}");
    }
}
