using System;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Launcher.App.ViewModels;
using Launcher.Core;
using Launcher.Providers;
using Launcher.Providers.Art;
using Microsoft.Extensions.DependencyInjection;

namespace Launcher.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settings = LauncherSettings.Load();

        // One shared HttpClient for the whole app. It carries no credentials: the
        // SteamGridDB key is attached per request, only to SteamGridDB's API.
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("GameLauncher/0.1");

        // The one place that knows about every piece. Adding GOG later = one more line here.
        var services = new ServiceCollection();
        services.AddSingleton(settings);
        services.AddSingleton(http);
        services.AddSingleton<ILibraryProvider, SteamProvider>();
        services.AddSingleton<ILibraryProvider, EpicProvider>();
        services.AddSingleton<LibraryService>();
        services.AddSingleton<IArtService>(sp =>
            ArtService.CreateDefault(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<LauncherSettings>()));
        services.AddSingleton<MainViewModel>();
        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = provider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = viewModel };

            // Fire and forget: LoadAsync reports its own errors through Status.
            _ = viewModel.LoadCommand.ExecuteAsync(null);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
