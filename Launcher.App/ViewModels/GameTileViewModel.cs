using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Launcher.Core;

namespace Launcher.App.ViewModels;

/// <summary>What one tile in the grid binds to.</summary>
public sealed class GameTileViewModel
{
    public GameTileViewModel(Game game, Action<Game> launch)
    {
        Game = game;
        LaunchCommand = new RelayCommand(() => launch(game));
    }

    public Game Game { get; }

    public string Title => Game.Title;

    public string StoreLabel => Game.Store.ToString();

    public ICommand LaunchCommand { get; }
}
