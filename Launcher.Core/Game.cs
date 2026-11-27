namespace Launcher.Core;

public enum GameStore {
    Steam,
    EpicGames
}

public sealed record Game(string Id, string Title, GameStore Store, string LaunchUri, string? InstallPath);