using System;
using System.Runtime.InteropServices;
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
            string? groupsConf = args.Length > 2 ? args[2] : null;
            Console.WriteLine(Diagnostics.BuildDumpJson(hookDir, groupsConf));
            return 0;
        }

        // GUI launch is wired up in Task 7.
        return 0;
    }
}
