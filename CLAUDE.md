# CLAUDE.md — peon-ping-tray

Windows system-tray companion for peon-ping (port of the macOS peon-ping-menubar SwiftBar plugin).

## Build / test / run
- Prereq: .NET 10 SDK (`dotnet --list-sdks` shows 10.x). End users need the .NET 10 Desktop Runtime.
- Build:  `powershell -File build.ps1`  → `PeonPingTray\bin\publish\PeonPingTray.exe` (framework-dependent single-file)
- Test:   `powershell -File tests\run-tests.ps1` (self-contained; no Pester)
- Install/uninstall: `powershell -File install.ps1` / `uninstall.ps1`

## Conventions
- C# on net10.0-windows, WinForms, Nullable enabled, ImplicitUsings disabled (explicit usings).
- GUI-independent logic in `PeonPingTray/Core/`; verified headlessly via exe modes `--dump` / `--run-peon` / `--icon-selftest`.
- State source of truth: peon-ping `config.json` `enabled` (true=ON, false=OFF). No `.paused` marker on Windows.
- Actions shell out to `%USERPROFILE%\.claude\hooks\peon-ping\peon.ps1`.
- The test runner invokes the (WinExe) exe via `Start-Process -Wait -RedirectStandardOutput` — a plain `& $exe` does not reliably wait for / capture a GUI-subsystem process.

## Not in git
- `docs/superpowers/` (specs & plans) and build output are git-ignored.

## Attribution
- Tray image (`Resources/peon.png`/`.ico`) is Blizzard's Orc Peon — third-party, not MIT.
