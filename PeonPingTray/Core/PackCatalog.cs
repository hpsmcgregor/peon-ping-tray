using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PeonPingTray.Core;

public sealed class PackInfo
{
    public string Id = "";
    public string DisplayName = "";
    public string? PreviewSound;
}

public static class PackCatalog
{
    public static List<PackInfo> Discover(string hookDir)
    {
        var result = new List<PackInfo>();
        string packsDir = Paths.PacksDir(hookDir);
        if (!Directory.Exists(packsDir)) return result;

        string[] dirs = Directory.GetDirectories(packsDir);
        Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);

        foreach (string dir in dirs)
        {
            string manifest = Path.Combine(dir, "openpeon.json");
            if (!File.Exists(manifest)) continue;

            var info = new PackInfo { Id = Path.GetFileName(dir) };
            info.DisplayName = info.Id;

            try
            {
                string text = File.ReadAllText(manifest).TrimStart('﻿');
                using JsonDocument doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("display_name", out JsonElement dn) &&
                    dn.ValueKind == JsonValueKind.String)
                {
                    string? v = dn.GetString();
                    if (!string.IsNullOrEmpty(v)) info.DisplayName = v;
                }
            }
            catch { /* keep id as display name */ }

            string soundsDir = Path.Combine(dir, "sounds");
            if (Directory.Exists(soundsDir))
            {
                string[] sounds = Directory.GetFiles(soundsDir);
                Array.Sort(sounds, StringComparer.OrdinalIgnoreCase);
                foreach (string f in sounds)
                {
                    if (IsAudio(Path.GetExtension(f))) { info.PreviewSound = f; break; }
                }
            }
            result.Add(info);
        }
        return result;
    }

    // Sound formats peon-ping's win-play.ps1 can play (wav/mp3/wma natively;
    // ogg/flac/etc. via its ffplay/mpv/vlc fallback chain).
    static bool IsAudio(string ext)
    {
        switch (ext.ToLowerInvariant())
        {
            case ".wav":
            case ".mp3":
            case ".wma":
            case ".ogg":
            case ".flac":
            case ".m4a":
            case ".aac":
            case ".aiff":
                return true;
            default:
                return false;
        }
    }
}
