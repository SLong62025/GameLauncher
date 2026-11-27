using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Launcher.Core;

public sealed class LauncherSettings {
    public string? SteamGridDBApiKey { get; init; }

    public static LauncherSettings Load(string? settingsPath = null)
    {
        settingsPath ??= AppPaths.SettingsFile;
        string? key = null;

        try
        {
            if (File.Exists(settingsPath)) {
                var options = new JsonDocumentOptions {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };

                using var doc = JsonDocument.Parse(File.ReadAllText(settingsPath), options);
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("SteamGridDbApiKey", out var value) &&
                    value.ValueKind == JsonValueKind.String) {
                    
                    key = value.GetString();
                }
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) {
            Debug.WriteLine($"Could not read settings: {e.Message}");
        }

        var fromEnvironment = Environment.GetEnvironmentVariable("STEAMGRIDDB_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnvironment)) key = fromEnvironment;

        key = key?.Trim();
        return new LauncherSettings { SteamGridDBApiKey = string.IsNullOrEmpty(key) ? null : key };
    }
}
