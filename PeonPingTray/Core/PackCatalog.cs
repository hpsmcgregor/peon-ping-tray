using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PeonPingTray.Core;

public sealed class PackInfo
{
    public string Id = "";
    public string DisplayName = "";
    public string? PreviewWav;
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
                string[] wavs = Directory.GetFiles(soundsDir, "*.wav");
                Array.Sort(wavs, StringComparer.OrdinalIgnoreCase);
                if (wavs.Length > 0) info.PreviewWav = wavs[0];
            }
            result.Add(info);
        }
        return result;
    }
}
