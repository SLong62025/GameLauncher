using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core;

namespace Launcher.Providers;

public sealed class SteamProvider : ILibraryProvider {
    //Only fetch fully installed titles
    private const int StateFullyInstalled = 4;

    //Exclude Steamworks Common Redistributables
    private static readonly HashSet<string> IgnoredAppIds = new() { "228980" };

    public GameStore Store => GameStore.Steam;

    public Task<IReadOnlyList<Game>> ScanInstalledAsync(CancellationToken ct = default) =>
        Task.Run(() => Scan(ct), ct);

    public void Launch(Game game) => ShellLauncher.Open(game.LaunchUri);

    private static IReadOnlyList<Game> Scan(CancellationToken ct) {
        var steamPath = SteamLocator.FindSteamPath();
        if (steamPath is null) return Array.Empty<Game>();

        var games = new Dictionary<string, Game>();

        foreach (var library in FindLibraries(steamPath)) {
            var steamApps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(steamApps)) continue;

            foreach (var manifest in Directory.EnumerateFiles(steamApps, "appmanifest_*.acf")) {
                ct.ThrowIfCancellationRequested();

                var game = TryReadManifest(manifest, steamApps);
                if (game is not null) games.TryAdd(game.Id, game);
            }
        }

        return games.Values.ToList();
    }

    //Read from libraryfolders.vdf.
    private static IEnumerable<string> FindLibraries(string steamPath) {
        var libraries = new List<string> { steamPath };
        try {
            var vdf = File.ReadAllText(Path.Combine(steamPath, "steamapps", "libraryfolders.vdf"));
            var folders = VdfParser.Parse(vdf).GetNode("libraryfolders");
            if (folders is not null) {
                foreach (var entry in folders.Children.Values) {
                    var path = entry.GetString("path");
                    if (!string.IsNullOrWhiteSpace(path)) {
                        libraries.Add(Path.GetFullPath(path));
                    }
                }
            }
        }
        catch (Exception e) when (e is IOException or FormatException or UnauthorizedAccessException or ArgumentException) {

        }

        return libraries.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static Game? TryReadManifest(string manifestPath, string steamApps) {
        try
        {
            var state = VdfParser.Parse(File.ReadAllText(manifestPath)).GetNode("AppState");
            var id = state?.GetString("appid");
            var name = state?.GetString("name");

            if (state is null || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name)) return null;
            if (!id.All(char.IsDigit) || IgnoredAppIds.Contains(id)) return null;

            if (int.TryParse(state.GetString("StateFlags"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var flags)
                && (flags & StateFullyInstalled) == 0) {
                return null;
            }

            var installDir = state.GetString("installdir");
            var installPath = string.IsNullOrEmpty(installDir) ? null : Path.Combine(steamApps, "common", installDir);

            return new Game(id, name, GameStore.Steam, $"steam://rungameid/{id}", installPath);
        }
        catch (Exception e) when (e is IOException or FormatException or UnauthorizedAccessException) {
            return null;
        }
    }
}
