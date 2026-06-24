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

        if (mode == "--run-peon")
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            string hookDir = args.Length > 1 ? args[1] : Paths.DefaultHookDir();
            string[] rest = args.Length > 2 ? args[2..] : Array.Empty<string>();
            bool ok = PeonCli.Run(hookDir, rest);
            Console.WriteLine(ok ? "OK" : "FAIL");
            return ok ? 0 : 1;
        }

        if (mode == "--icon-selftest")
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            using var on = IconFactory.Create("ON");
            using var off = IconFactory.Create("OFF");
            using var unk = IconFactory.Create("X");
            Console.WriteLine($"{on.Width}x{on.Height} {off.Width}x{off.Height} {unk.Width}x{unk.Height}");
            return 0;
        }

        // GUI launch is wired up in Task 7.
        return 0;
    }
}
