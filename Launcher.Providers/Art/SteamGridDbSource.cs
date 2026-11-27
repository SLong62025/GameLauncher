using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core;

namespace Launcher.Providers.Art;

//Epic needs free api key
public sealed class SteamGridDbSource : IArtSource
{
    private const string ApiBase = "https://www.steamgriddb.com/api/v2";

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public SteamGridDbSource(HttpClient http, string apiKey) {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API key is required.", nameof(apiKey));
        _http = http;
        _apiKey = apiKey;
    }

    public string Name => "SteamGridDB";

    public bool CanHandle(Game game) => true;

    public async Task<byte[]?> FetchFile(Game game, CancellationToken ct) {
        int? gameId;
        if (game.Store == GameStore.Steam && game.Id.All(char.IsDigit))
        {
            // The Steam appid identifies the game exactly, so don't guess by title.
            gameId = await FindGameIdBySteamAppAsync(game.Id, ct);
        }
        else
        {
            gameId = await FindGameIdByTitleAsync(game.Title, ct);
        }

        if (gameId is null) return null;

        var imageUrl = await FindCoverUrlAsync(gameId.Value, ct);
        if (imageUrl is null) return null;

        // Only download from SteamGridDB's own hosts, whatever the API response says.
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !(uri.Host == "steamgriddb.com" || uri.Host.EndsWith(".steamgriddb.com", StringComparison.OrdinalIgnoreCase))) {
            
            return null;
        }

        // Plain download with no Authorization header: the key only ever goes to the API.
        return await HttpFetch.GetBytesAsync(_http, uri.AbsoluteUri, ct);
    }

    private async Task<int?> FindGameIdBySteamAppAsync(string appId, CancellationToken ct) {
        using var doc = await GetApiAsync($"/games/steam/{appId}", ct);
        if (doc is null) return null;

        var data = doc.RootElement.GetProperty("data");
        if (data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("id", out var id) && id.TryGetInt32(out var value))
        {
            return value;
        }

        return null;
    }

    private async Task<int?> FindGameIdByTitleAsync(string title, CancellationToken ct) {
        var term = Normalize(title);
        if (term.Length == 0) return null;

        using var doc = await GetApiAsync("/search/autocomplete/" + Uri.EscapeDataString(term), ct);
        if (doc is null) return null;

        var data = doc.RootElement.GetProperty("data");
        if (data.ValueKind != JsonValueKind.Array) return null;
        
        int? first = null;
        foreach (var item in data.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var idElement) || !idElement.TryGetInt32(out var id)) continue;

            first ??= id;

            if (item.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String &&
                string.Equals(Normalize(nameElement.GetString()), term, StringComparison.OrdinalIgnoreCase)) {
                
                return id;
            }
        }

        return first;
    }

    private async Task<string?> FindCoverUrlAsync(int gameId, CancellationToken ct) {
        // Portrait grids only: a landscape image cropped into a cover tile looks bad.
        using var doc = await GetApiAsync(
            $"/grids/game/{gameId}?dimensions=600x900,342x482,660x930&types=static&nsfw=false&humor=false", ct);
        if (doc is null) return null;

        var data = doc.RootElement.GetProperty("data");
        if (data.ValueKind != JsonValueKind.Array) return null;

        foreach (var grid in data.EnumerateArray())
        {
            if (grid.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String)
                return url.GetString();
        }

        return null;
    }

    /// <summary>GET an API path. Null means "not found"; other failures throw.</summary>
    private async Task<JsonDocument?> GetApiAsync(string pathAndQuery, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiBase + pathAndQuery);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await _http.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();   // a wrong key (401) surfaces here

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

        if (doc.RootElement.ValueKind != JsonValueKind.Object ||
            !doc.RootElement.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True ||
            !doc.RootElement.TryGetProperty("data", out _))
        {
            doc.Dispose();
            return null;
        }

        return doc;
    }

    // "LEGO® Fortnite" -> "LEGO Fortnite": trademark marks break name matching.
    private static string Normalize(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        var cleaned = title.Replace("®", "").Replace("™", "").Replace("©", "");
        return string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
