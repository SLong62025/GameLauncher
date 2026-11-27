using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Launcher.Providers;

internal static class SteamLocator {

    public static string? FindSteamPath() {
        string? fromRegistry = null;
        if (OperatingSystem.IsWindows()) {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            fromRegistry = key?.GetValue("SteamPath") as string;
        }

        var candidates = new[] {
            fromRegistry,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
        };

        return candidates
            .Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => Path.GetFullPath(p!)).FirstOrDefault(p => File.Exists(Path.Combine(p, "steamapps", "libraryfolders.vdf")));
    }
}
