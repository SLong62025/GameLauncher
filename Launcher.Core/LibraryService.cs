using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Core;


public sealed class LibraryService {
    private readonly Dictionary<GameStore, ILibraryProvider> _providers;

    public LibraryService(IEnumerable<ILibraryProvider> providers) {
        _providers = providers.ToDictionary(p => p.Store);
    }
    
    //Multithreading things
    public async Task<IReadOnlyList<Game>> GetLibraryAsync(CancellationToken ct = default) {
        var scans = _providers.Values.Select(p => ScanSafelyAsync(p, ct));
        var results = await Task.WhenAll(scans);

        return results
            .SelectMany(games => games)
            .OrderBy(g => g.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public void Launch(Game game) {
        if (!_providers.TryGetValue(game.Store, out var provider))
            throw new InvalidOperationException($"No provider registered for {game.Store}.");

        provider.Launch(game);
    }
    
    private static async Task<IReadOnlyList<Game>> ScanSafelyAsync(ILibraryProvider provider, CancellationToken ct) {
        try
        {
            return await provider.ScanInstalledAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"{provider.Store} scan failed: {ex}");
            return Array.Empty<Game>();
        }
    }
}