using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using PeonPingTray.Core;

namespace PeonPingTray;

static class Program
{
    [DllImport("kernel32.dll")] static extern bool AttachConsole(int dwProcessId);
    const int ATTACH_PARENT_PROCESS = -1;

    [STAThread]
    static int Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0] : "";

        if (mode == "--dump")
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            string hookDir = args.Length > 1 ? args[1] : Paths.DefaultHookDir();
            // Task 2 swaps this for Diagnostics.BuildDumpJson(hookDir, groupsConf).
            var map = new Dictionary<string, object?> { ["hookDir"] = hookDir };
            Console.WriteLine(JsonSerializer.Serialize(map));
            return 0;
        }

        // GUI launch is wired up in Task 7.
        return 0;
    }
}
