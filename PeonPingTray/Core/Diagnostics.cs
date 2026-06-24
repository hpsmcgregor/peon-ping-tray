using System.Collections.Generic;
using System.Text.Json;

namespace PeonPingTray.Core;

public static class Diagnostics
{
    public static string BuildDumpJson(string hookDir, string? groupsConfPath)
    {
        var dump = new Dictionary<string, object?>();
        dump["hookDir"] = hookDir;

        var cfg = PeonConfig.Read(hookDir);
        dump["configFound"] = cfg.Found;
        dump["enabled"] = cfg.Enabled;          // bool? -> true/false/null
        dump["state"] = cfg.State;
        dump["defaultPack"] = cfg.DefaultPack;
        dump["volume"] = cfg.Volume;

        var packs = PackCatalog.Discover(hookDir);
        var packDump = new List<object?>();
        foreach (var p in packs)
        {
            packDump.Add(new Dictionary<string, object?>
            {
                ["id"] = p.Id,
                ["displayName"] = p.DisplayName,
                ["previewWav"] = p.PreviewWav,
                ["isCurrent"] = p.Id == cfg.DefaultPack
            });
        }
        dump["packs"] = packDump;

        return JsonSerializer.Serialize(dump);
    }
}
