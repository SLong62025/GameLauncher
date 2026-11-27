using System;
using System.IO;

namespace Launcher.Core;

public static class AppPaths {
    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GameLauncher");

    public static string CoverCacheDir => Path.Combine(DataDir, "covers");

    public static string SettingsFile => Path.Combine(DataDir, "settings.json");
}
