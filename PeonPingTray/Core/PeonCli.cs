using System;
using System.Diagnostics;
using System.IO;

namespace PeonPingTray.Core;

public static class PeonCli
{
    public static bool Run(string hookDir, params string[] peonArgs)
    {
        string script = Paths.PeonScript(hookDir);
        if (!File.Exists(script))
        {
            Log("peon.ps1 not found at " + script);
            return false;
        }

        string argline = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"";
        foreach (string a in peonArgs) argline += " " + Quote(a);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = argline,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        try
        {
            using Process? p = Process.Start(psi);
            if (p is null) { Log("Process.Start returned null"); return false; }
            string err = p.StandardError.ReadToEnd();
            p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                Log("peon.ps1 " + string.Join(" ", peonArgs) + " exited " + p.ExitCode + ": " + err);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Log("Failed to run peon.ps1: " + ex.Message);
            return false;
        }
    }

    static string Quote(string s) => s.IndexOf(' ') < 0 ? s : "\"" + s + "\"";

    static void Log(string msg)
    {
        try
        {
            Directory.CreateDirectory(Paths.AppDataDir());
            File.AppendAllText(Paths.LogFile(), DateTime.Now.ToString("s") + " " + msg + Environment.NewLine);
        }
        catch { }
    }
}
