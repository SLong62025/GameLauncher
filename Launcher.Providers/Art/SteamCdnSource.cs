using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core;

namespace Launcher.Providers.Art;

public sealed class SteamCdnSource : IArtSource {
    //Screw you valve
    private static readonly string[] BaseUrls = {
        "https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps",
        "https://cdn.cloudflare.steamstatic.com/steam/apps"
    };

    private readonly HttpClient _http;

    public SteamCdnSource(HttpClient http) => _http = http;

    public string Name => "Steam CDN";

    public bool CanHandle(Game game) =>
        game.Store == GameStore.Steam && game.Id.Length > 0 && game.Id.All(char.IsDigit);

    public async Task<byte[]?> FetchFile(Game game, CancellationToken ct) {
        Exception? failure = null;

        foreach (var baseUrl in BaseUrls) {
            try {
                var bytes = await HttpFetch.GetBytesAsync(_http, $"{baseUrl}/{game.Id}/library_600x900.jpg", ct, treatForbiddenAsMissing: true);
                if (bytes is not null) return bytes;
            }
            catch (HttpRequestException ex) {
                failure = ex;
            }
        }

        if (failure is not null) throw new HttpRequestException("Steam CDN request failed.", failure);
        return null;
    }
}
