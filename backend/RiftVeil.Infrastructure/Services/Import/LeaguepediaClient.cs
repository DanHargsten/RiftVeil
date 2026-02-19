using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Client for querying Leaguepedia Cargo API endpoints and returning raw JSON rows.
/// Uses api.php instead of CargoExport because Leaguepedia had strict bot detection
/// that blocked the CargoExport endpoint; api.php works correctly.
/// </summary>
public class LeaguepediaClient(HttpClient httpClient, IHostEnvironment environment)
{
    private const string BaseUrl = "https://lol.fandom.com/api.php";

    /// <summary>
    /// Queries Leaguepedia Cargo export API with URL-encoded parameters.
    /// </summary>
    /// <param name="tables">Cargo tables to query.</param>
    /// <param name="fields">Field projection returned by Cargo.</param>
    /// <param name="where">Optional Cargo where clause.</param>
    /// <param name="orderBy">Optional Cargo order by clause.</param>
    /// <param name="limit">Maximum number of rows to return.</param>
    /// <returns>List of raw JSON elements from Cargo response.</returns>
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
            query += $"&where={Uri.EscapeDataString(where)}";

        if (!string.IsNullOrEmpty(orderBy))
            query += $"&order_by={Uri.EscapeDataString(orderBy)}";

        var url = BaseUrl + query;
        if (environment.IsDevelopment())
        {
            Console.WriteLine($"Fetching: {url}");
        }

        var response = await httpClient.GetAsync(url);
        if (environment.IsDevelopment())
        {
            Console.WriteLine($"Status: {response.StatusCode}");
        }
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        // Strip UTF-8 BOM if present
        if (json.Length > 0 && json[0] == '\uFEFF')
            json = json[1..];

        using var doc = JsonDocument.Parse(json);

        var results = doc.RootElement
            .GetProperty("cargoquery")
            .EnumerateArray()
            .Select(item => item.GetProperty("title").Clone())
            .ToList();

        return results;
    }
}
