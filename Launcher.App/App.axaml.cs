using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Launcher.App.ViewModels;
using Launcher.Core;
using Launcher.Providers;
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
        // The one place that knows about every piece. Adding GOG later = one more line here.
        var services = new ServiceCollection();
        services.AddSingleton<ILibraryProvider, SteamProvider>();
        services.AddSingleton<ILibraryProvider, EpicProvider>();
        services.AddSingleton<LibraryService>();
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
