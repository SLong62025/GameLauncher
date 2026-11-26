using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Core;

namespace Launcher.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly LibraryService _library;
    private string _status = "Loading...";

    public MainViewModel(LibraryService library)
    {
        _library = library;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ObservableCollection<GameTileViewModel> Games { get; } = new();

    public IAsyncRelayCommand LoadCommand { get; }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    private async Task LoadAsync()
    {
        Status = "Scanning...";
        try
        {
            var games = await _library.GetLibraryAsync();

            Games.Clear();
            foreach (var game in games)
                Games.Add(new GameTileViewModel(game, LaunchGame));

            Status = games.Count == 0
                ? "No installed games found. Is Steam or Epic installed?"
                : $"{games.Count} games";
        }
        catch (Exception ex)
        {
            Status = $"Scan failed: {ex.Message}";
        }
    }

    private void LaunchGame(Game game)
    {
        try
        {
            _library.Launch(game);
            Status = $"Launching {game.Title}...";
        }
        catch (Exception ex)
        {
            Status = $"Couldn't launch {game.Title}: {ex.Message}";
        }
    }
}
