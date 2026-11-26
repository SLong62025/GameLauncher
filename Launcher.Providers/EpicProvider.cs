using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core;

namespace Launcher.Providers;

public sealed class EpicProvider : ILibraryProvider {
    public GameStore Store => GameStore.EpicGames;

    public Task<IReadOnlyList<Game>> ScanInstalledAsync(CancellationToken ct = default) => Task.Run(() => Scan(ct), ct);
    
    public void Launch(Game game) => ShellLauncher.Open(game.LaunchUri);
    
    //Read Game List and return a list of games
    private static IReadOnlyList<Game> Scan(CancellationToken ct) {
        var manifestDir = Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");

        if (!Directory.Exists(manifestDir)) return Array.Empty<Game>();

        var games = new Dictionary<string, Game>();

        foreach (var file in Directory.EnumerateFiles(manifestDir, "*.item")) {
            ct.ThrowIfCancellationRequested();

            var game = TryReadManifest(file);
            if (game is not null) games.TryAdd(game.Id, game);
        }

        return games.Values.ToList();
    }
    //Read Manifest @ \ProgramData\Epic\EpicGamesLauncher\Data\Manifests
    private static Game? TryReadManifest(string path) {
        try {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            string? Str(string name) =>
                root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

            var appName = Str("AppName");
            var catalogNamespace = Str("CatalogNamespace");
            var catalogItemId = Str("CatalogItemId");
            var title = Str("DisplayName");

            if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(catalogNamespace) ||
                string.IsNullOrEmpty(catalogItemId) || string.IsNullOrEmpty(title)) {
                return null;
            }
            
            var mainApp = Str("MainGameAppName");
            if (!string.IsNullOrEmpty(mainApp) && !string.Equals(mainApp, appName, StringComparison.OrdinalIgnoreCase)) {
                return null;
            }

            if (root.TryGetProperty("bIsIncompleteInstall", out var incomplete) &&
                incomplete.ValueKind == JsonValueKind.True) {
                return null;
            }

            
            var uri = "com.epicgames.launcher://apps/" + $"{Uri.EscapeDataString(catalogNamespace)}%3A" + 
                      $"{Uri.EscapeDataString(catalogItemId)}%3A" + $"{Uri.EscapeDataString(appName)}" + "?action=launch&silent=true";

            return new Game(appName, title, GameStore.EpicGames, uri, Str("InstallLocation"));
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) {
            Debug.WriteLine($"Skipping {path}: {ex.Message}");
            return null;
        }
    }
}
