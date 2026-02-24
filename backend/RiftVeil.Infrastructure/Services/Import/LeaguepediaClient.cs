using System.Text.Json;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Client for querying Leaguepedia data via the MediaWiki API.
/// </summary>
public class LeaguepediaClient(HttpClient httpClient)
{
    private const string BaseUrl = "https://lol.fandom.com/api.php";

    /// <summary>
    /// Executes a query and returns the result rows as JSON elements.
    /// </summary>
    /// <param name="tables">Table(s) to query.</param>
    /// <param name="fields">Fields to return.</param>
    /// <param name="where">Optional WHERE clause.</param>
    /// <param name="orderBy">Optional ORDER BY clause.</param>
    /// <param name="limit">Maximum number of rows (default 50).</param>
    public async Task<List<JsonElement>> QueryAsync(
        string tables,
        string fields,
        string? where = null,
        string? orderBy = null,
        int limit = 50)
    {
        var query = $"?action=cargoquery" +
                    $"&tables={Uri.EscapeDataString(tables)}" +
                    $"&fields={Uri.EscapeDataString(fields)}" +
                    $"&format=json" +
                    $"&limit={limit}";

        if (!string.IsNullOrEmpty(where))
        {
            query += $"&where={Uri.EscapeDataString(where)}";
        }

        if (!string.IsNullOrEmpty(orderBy))
        {
            query += $"&order_by={Uri.EscapeDataString(orderBy)}";
        }

        var url = BaseUrl + query;
        Console.WriteLine($"Fetching: {url}");

        for (var attempt = 0; attempt < 8; attempt++)
        {
            if (attempt > 0)
            {
                // Exponential backoff: 10s, 20s, 30s, 45s, 60s, 90s, 120s
                var delay = attempt switch
                {
                    1 => 10_000,
                    2 => 20_000,
                    3 => 30_000,
                    4 => 45_000,
                    5 => 60_000,
                    6 => 90_000,
                    _ => 120_000
                };
                Console.WriteLine($"  Rate limited (attempt {attempt + 1}/8), waiting {delay / 1000}s...");
                await Task.Delay(delay);
            }

            var response = await httpClient.GetAsync(url);
            Console.WriteLine($"  Status: {response.StatusCode}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            if (json.Length > 0 && json[0] == '\uFEFF')
            {
                json = json[1..];
            }

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var code = error.GetProperty("code").GetString();
                var info = error.TryGetProperty("info", out var infoProp) ? infoProp.GetString() : "unknown";
                
                if (code == "ratelimited")
                {
                    Console.WriteLine($"  Rate limited by API (attempt {attempt + 1}/8)");
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
            return results;
        }

        Console.WriteLine("  Max retries exceeded");
        return [];
    }
}