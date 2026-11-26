using System;
using System.Diagnostics;
using System.Linq;

namespace Launcher.Providers;

//Allows Windows to launch the game based on the allowed prefixes
internal static class ShellLauncher {
    private static readonly string[] AllowedPrefixes = {
        "steam://",
        "com.epicgames.launcher://"
    };
    
    public static void Open(string uri)
    {
        if (!AllowedPrefixes.Any(p => uri.StartsWith(p, StringComparison.OrdinalIgnoreCase))) {
            throw new InvalidOperationException();
        }
        
        //Let Windows do its thing
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
}