using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Core;

namespace Launcher.Providers.Art;

public sealed class ArtService : IArtService {
    private const int MaxParallelFetches = 4;
    private const string MissSuffix = ".miss";
    private static readonly TimeSpan MissRetryAfter = TimeSpan.FromDays(7);

    private readonly IReadOnlyList<IArtSource> _sources;
    private readonly string _cacheDir;
    private readonly SemaphoreSlim _gate = new(MaxParallelFetches);
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _inFlight = new();

    public ArtService(IEnumerable<IArtSource> sources, string? cacheDir = null)
    {
        _sources = sources.ToList();
        _cacheDir = cacheDir ?? AppPaths.CoverCacheDir;
    }
    
    //Use Steam Cache -> Fall back to SteamGridDB if nothing
    public static ArtService CreateDefault(HttpClient http, LauncherSettings settings)
    {
        var sources = new List<IArtSource> {
            new LocalSteamCacheSource(),
            new SteamCdnSource(http)
        };

        if (settings.SteamGridDBApiKey is { } key)
            sources.Add(new SteamGridDbSource(http, key));

        return new ArtService(sources);
    }

    public async Task<string?> GetCoverPath(Game game, CancellationToken ct = default) {
        var path = CachePath(game);
        if (File.Exists(path)) return path;

        var signature = SourceSignature(game);
        if (signature.Length == 0) return null;     
        if (IsRecentMiss(path, signature)) return null; 
        
        var lazy = _inFlight.GetOrAdd(path,
            _ => new Lazy<Task<string?>>(() => FetchAndCacheAsync(game, path, signature)));
        return await lazy.Value.WaitAsync(ct);
    }

    private async Task<string?> FetchAndCacheAsync(Game game, string path, string signature) {
        try {
            await _gate.WaitAsync();
            try {
                return await FetchFromSourcesAsync(game, path, signature);
            }
            finally {
                _gate.Release();
            }
        }
        finally {
            _inFlight.TryRemove(path, out _);
        }
    }

    private async Task<string?> FetchFromSourcesAsync(Game game, string path, string signature) {
        var hadError = false;

        foreach (var source in _sources.Where(s => s.CanHandle(game))) {
            byte[]? bytes;
            try {
                bytes = await source.FetchFile(game, CancellationToken.None);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException
                                          or UnauthorizedAccessException or JsonException) {
                hadError = true;
                Debug.WriteLine($"{source.Name} failed for {game.Title}: {ex.Message}");
                continue;
            }

            if (bytes is null || !ImageSniffer.LooksLikeImage(bytes)) continue;

            if (!TryWrite(path, bytes)) return null;

            DeleteQuietly(path + MissSuffix);
            return path;
        }
        
        if (!hadError) TryWriteMissMarker(path, signature);
        return null;
    }

    private string CachePath(Game game) {
        var safeId = new string(game.Id.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());
        return Path.Combine(_cacheDir, $"{game.Store}_{safeId}.img");
    }
    
    private string SourceSignature(Game game) => string.Join('|', _sources.Where(s => s.CanHandle(game)).Select(s => s.Name));

    private static bool IsRecentMiss(string path, string signature) {
        var marker = path + MissSuffix;
        try {
            return File.Exists(marker)
                && DateTime.UtcNow - File.GetLastWriteTimeUtc(marker) < MissRetryAfter
                && File.ReadAllText(marker) == signature;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            return false;
        }
    }

    private static bool TryWrite(string path, byte[] bytes) {
        var temp = $"{path}.{Guid.NewGuid():N}.tmp";
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(temp, bytes);
            File.Move(temp, path, overwrite: true); 
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Debug.WriteLine($"Could not cache cover: {ex.Message}");
            DeleteQuietly(temp);
            return false;
        }
    }

    private static void TryWriteMissMarker(string path, string signature) {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path + MissSuffix, signature);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) {

        }
    }

    private static void DeleteQuietly(string path) {
        try { File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
