using System;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Core;

namespace Launcher.App.ViewModels;

/// <summary>What one tile in the grid binds to.</summary>
public sealed class GameTileViewModel : ObservableObject
{
    private Bitmap? _cover;

    public GameTileViewModel(Game game, Action<Game> launch)
    {
        Game = game;
        LaunchCommand = new RelayCommand(() => launch(game));
    }

    public Game Game { get; }

    public string Title => Game.Title;

    public string StoreLabel => Game.Store.ToString();

    public ICommand LaunchCommand { get; }

    /// <summary>Null until the cover has loaded (or forever, if the game has none).</summary>
    public Bitmap? Cover
    {
        get => _cover;
        set
        {
            if (SetProperty(ref _cover, value))
                OnPropertyChanged(nameof(HasCover));
        }
    }

    public bool HasCover => _cover is not null;

    /// <summary>Frees the decoded image. Call when the tile is discarded.</summary>
    public void ReleaseCover()
    {
        var cover = _cover;
        Cover = null;
        cover?.Dispose();
    }
}
