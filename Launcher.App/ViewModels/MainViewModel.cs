using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Core;

namespace Launcher.App.ViewModels;

public sealed class MainViewModel : ObservableObject {
    private const int CoverDecodeWidth = 300;
    private readonly LibraryService _library;
    private readonly IArtService _art;
    private readonly SemaphoreSlim _decodeGate = new(4);
    private CancellationTokenSource? _coversCts;
    private string _status = "Loading...";

    public MainViewModel(LibraryService library, IArtService art) {
        _library = library;
        _art = art;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ObservableCollection<GameTileViewModel> Games { get; } = new();

    public IAsyncRelayCommand LoadCommand { get; }

    public string Status {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    private async Task LoadAsync() {
        _coversCts?.Cancel();

        Status = "Scanning...";
        try {
            var games = await _library.GetLibraryAsync();

            var previous = Games.ToList();
            Games.Clear();
            foreach (var tile in previous) tile.ReleaseCover();

            var tiles = games.Select(g => new GameTileViewModel(g, LaunchGame)).ToList();
            foreach (var tile in tiles) Games.Add(tile);

            Status = games.Count == 0
                ? "No installed games found. Is Steam or Epic installed?"
                : $"{games.Count} games";
            _coversCts = new CancellationTokenSource();
            _ = LoadCoversAsync(tiles, _coversCts.Token);
        }
        catch (Exception e) {
            
        }
    }

    private Task LoadCoversAsync(IReadOnlyList<GameTileViewModel> tiles, CancellationToken ct) => Task.WhenAll(tiles.Select(tile => LoadCoverAsync(tile, ct)));
    private async Task LoadCoverAsync(GameTileViewModel tile, CancellationToken ct) {
        try {
            var path = await _art.GetCoverPath(tile.Game, ct);
            if (path is null || ct.IsCancellationRequested) return;

            Bitmap bitmap;
            await _decodeGate.WaitAsync(ct);
            try {
                bitmap = await Task.Run(() => {
                    using var stream = File.OpenRead(path);
                    return Bitmap.DecodeToWidth(stream, CoverDecodeWidth);
                }, ct);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                try { File.Delete(path); } catch (IOException) { }
                return;
            }
            finally {
                _decodeGate.Release();
            }

            if (ct.IsCancellationRequested) {
                bitmap.Dispose(); 
                return;
            }

            tile.Cover = bitmap;
        }
        catch (OperationCanceledException) {
        }
        catch (Exception e) {
            
        }
    }

    private void LaunchGame(Game game) {
        try {
            _library.Launch(game);
            Status = $"Launching {game.Title}...";
        }
        catch (Exception e) {
            Status = $"Couldn't launch {game.Title}: {e.Message}";
        }
    }
}
