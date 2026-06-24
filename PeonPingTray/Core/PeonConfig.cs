using System.IO;
using System.Text.Json;

namespace PeonPingTray.Core;

public sealed class PeonConfig
{
    public bool Found;
    public bool? Enabled;
    public string? DefaultPack;
    public double Volume = 0.5;

    public string State
    {
        get
        {
            if (!Found || Enabled is null) return "UNKNOWN";
            return Enabled.Value ? "ON" : "OFF";
        }
    }

    public static PeonConfig Read(string hookDir)
    {
        var cfg = new PeonConfig();
        string path = Paths.ConfigJson(hookDir);
        if (!File.Exists(path)) return cfg;
        try
        {
            string text = File.ReadAllText(path).TrimStart('﻿'); // strip UTF-8 BOM
            using JsonDocument doc = JsonDocument.Parse(text);
            JsonElement root = doc.RootElement;
            cfg.Found = true;
            if (root.TryGetProperty("enabled", out JsonElement en) &&
                (en.ValueKind == JsonValueKind.True || en.ValueKind == JsonValueKind.False))
                cfg.Enabled = en.GetBoolean();
            if (root.TryGetProperty("default_pack", out JsonElement dp) && dp.ValueKind == JsonValueKind.String)
                cfg.DefaultPack = dp.GetString();
            if (root.TryGetProperty("volume", out JsonElement vol) && vol.ValueKind == JsonValueKind.Number)
                cfg.Volume = vol.GetDouble();
        }
        catch
        {
            cfg.Found = false;
            cfg.Enabled = null;
        }
        return cfg;
    }
}
