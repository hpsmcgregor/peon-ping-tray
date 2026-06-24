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
            // Drain stdout asynchronously while reading stderr to avoid a pipe-buffer deadlock.
            System.Threading.Tasks.Task<string> outTask = p.StandardOutput.ReadToEndAsync();
            string err = p.StandardError.ReadToEnd();
            outTask.Wait();
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

    // Play a preview sound via peon-ping's own win-play.ps1 (handles wav/mp3/wma and,
    // via its fallback chain, ogg/flac). Fire-and-forget: the script blocks for the
    // clip's duration, so we do not wait on it from the UI thread.
    public static void PlaySound(string hookDir, string soundPath, double volume)
    {
        string script = Path.Combine(hookDir, "scripts", "win-play.ps1");
        if (!File.Exists(script))
        {
            Log("win-play.ps1 not found at " + script);
            return;
        }

        string argline = "-NoProfile -ExecutionPolicy Bypass -File \"" + script +
            "\" -path \"" + soundPath + "\" -vol " +
            volume.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = argline,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try { Process.Start(psi); }
        catch (Exception ex) { Log("Failed to play preview: " + ex.Message); }
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
