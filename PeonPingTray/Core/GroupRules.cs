using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PeonPingTray.Core;

public sealed class GroupRules
{
    public const string Other = "Other";

    readonly List<KeyValuePair<string, string>> _rules = new();
    readonly List<string> _order = new();

    public bool HasRules => _rules.Count > 0;

    public static GroupRules Load(string? confPath)
    {
        var g = new GroupRules();
        if (string.IsNullOrEmpty(confPath) || !File.Exists(confPath)) return g;
        foreach (string raw in File.ReadAllLines(confPath))
        {
            string line = raw;
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            int eq = line.IndexOf('=');
            if (eq < 0) continue;
            string pat = line.Substring(0, eq).Trim();
            string lab = line.Substring(eq + 1).Trim();
            if (pat.Length == 0 || lab.Length == 0) continue;
            g._rules.Add(new KeyValuePair<string, string>(pat, lab));
            if (!g._order.Contains(lab)) g._order.Add(lab);
        }
        return g;
    }

    public string GroupFor(string id)
    {
        foreach (var r in _rules)
            if (GlobMatch(r.Key, id)) return r.Value;
        return Other;
    }

    public List<string> OrderedGroups(IEnumerable<string> ids)
    {
        var present = new HashSet<string>();
        foreach (string id in ids) present.Add(GroupFor(id));
        var ordered = new List<string>();
        foreach (string g in _order)
            if (present.Contains(g) && !ordered.Contains(g)) ordered.Add(g);
        if (present.Contains(Other)) ordered.Add(Other);
        return ordered;
    }

    static bool GlobMatch(string pattern, string input)
    {
        string rx = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(input, rx);
    }
}
