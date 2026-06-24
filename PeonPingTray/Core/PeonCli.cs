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

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        AddPwshArgs(psi, script, peonArgs);

        try
        {
            using Process? p = Process.Start(psi);
            if (p is null) { Log("Process.Start returned null"); return false; }
            // Drain stdout asynchronously while reading stderr to avoid a pipe-buffer deadlock.
            // GetAwaiter().GetResult() rethrows any read fault directly into the catch below
            // (unlike Wait(), which wraps it in AggregateException).
            System.Threading.Tasks.Task<string> outTask = p.StandardOutput.ReadToEndAsync();
            string err = p.StandardError.ReadToEnd();
            outTask.GetAwaiter().GetResult();
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

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        AddPwshArgs(psi, script, "-path", soundPath, "-vol",
            volume.ToString(System.Globalization.CultureInfo.InvariantCulture));

        try
        {
            // Dispose only the .NET Process handle; the powershell.exe child keeps
            // running independently until the clip finishes.
            using Process? proc = Process.Start(psi);
        }
        catch (Exception ex) { Log("Failed to play preview: " + ex.Message); }
    }

    // Build a verbatim argument vector (no manual quoting): ArgumentList escapes each
    // argument correctly, so paths containing spaces or quotes cannot break or inject.
    static void AddPwshArgs(ProcessStartInfo psi, string scriptPath, params string[] scriptArgs)
    {
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(scriptPath);
        foreach (string a in scriptArgs) psi.ArgumentList.Add(a);
    }

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
