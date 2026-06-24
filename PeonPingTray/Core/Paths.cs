using System;
using System.IO;

namespace PeonPingTray.Core;

public static class Paths
{
    public static string DefaultHookDir()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".claude", "hooks", "peon-ping");
    }

    public static string PeonScript(string hookDir) => Path.Combine(hookDir, "peon.ps1");
    public static string ConfigJson(string hookDir) => Path.Combine(hookDir, "config.json");
    public static string PacksDir(string hookDir) => Path.Combine(hookDir, "packs");

    public static string AppDataDir()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "PeonPingTray");
    }
    public static string GroupsConf() => Path.Combine(AppDataDir(), "peonping-groups.conf");
    public static string LogFile() => Path.Combine(AppDataDir(), "tray.log");
}
