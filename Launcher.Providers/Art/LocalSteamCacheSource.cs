using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core;

namespace Launcher.Providers.Art;


public sealed class LocalSteamCacheSource : IArtSource {
    private readonly Lazy<string?> _steamPath;

    public LocalSteamCacheSource(Func<string?>? steamPathProvider = null) {
        _steamPath = new Lazy<string?>(steamPathProvider ?? SteamLocator.FindSteamPath);
    }

    public string Name => "Steam local cache";
    
    public bool CanHandle(Game game) =>
        game.Store == GameStore.Steam && game.Id.Length > 0 && game.Id.All(char.IsDigit);

    public async Task<byte[]?> FetchFile(Game game, CancellationToken ct) {
        var steamPath = _steamPath.Value;
        if (steamPath is null) return null;

        var cacheDir = Path.Combine(steamPath, "appcache", "librarycache");
        if (!Directory.Exists(cacheDir)) return null;
        
        var flat = Path.Combine(cacheDir, $"{game.Id}_library_600x900.jpg");
        if (File.Exists(flat)) return await File.ReadAllBytesAsync(flat, ct);
        
        var appDir = Path.Combine(cacheDir, game.Id);
        if (Directory.Exists(appDir))
        {
            var nested = Directory
                .EnumerateFiles(appDir, "library_600x900.jpg", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (nested is not null) return await File.ReadAllBytesAsync(nested, ct);
        }

        return null;
    }
}
