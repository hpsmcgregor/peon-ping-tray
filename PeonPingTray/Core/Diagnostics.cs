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

        string confPath = string.IsNullOrEmpty(groupsConfPath) ? Paths.GroupsConf() : groupsConfPath;
        var rules = GroupRules.Load(confPath);

        var packs = PackCatalog.Discover(hookDir);
        var packDump = new List<object?>();
        foreach (var p in packs)
        {
            packDump.Add(new Dictionary<string, object?>
            {
                ["id"] = p.Id,
                ["displayName"] = p.DisplayName,
                ["previewSound"] = p.PreviewSound,
                ["group"] = rules.GroupFor(p.Id),
                ["isCurrent"] = p.Id == cfg.DefaultPack
            });
        }
        dump["packs"] = packDump;

        var ids = new List<string>();
        foreach (var p in packs) ids.Add(p.Id);
        dump["groupsOrder"] = rules.OrderedGroups(ids);

        return JsonSerializer.Serialize(dump);
    }
}
