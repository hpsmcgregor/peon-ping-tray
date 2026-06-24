# peon-ping-tray

> **Windows only.** A system-tray companion for [peon-ping](https://github.com/PeonPing/peon-ping) —
> the Windows counterpart to the macOS [peon-ping-menubar](https://github.com/hpsmcgregor/peon-ping-menubar) SwiftBar plugin.

Shows whether peon-ping notification sounds are on or muted, and lets you toggle
them and switch voice packs from the notification area.

- **Green dot** on the peon icon — sounds are **on**.
- **Red dot** — sounds are **muted**.
- **Grey dot** — state unknown (peon-ping not installed / config unreadable).
- Click (left or right) for a menu: mute/unmute, pick a sound pack (grouped by
  franchise, with per-pack preview), refresh, exit.

Unlike the macOS version there is **no host app to install** (no SwiftBar
equivalent): it's a single windowless `.exe`.

## Requirements

- Windows 10/11.
- **[.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)** to run
  (or the .NET 10 SDK to build).
- [peon-ping](https://github.com/PeonPing/peon-ping) installed, with its hook at
  `%USERPROFILE%\.claude\hooks\peon-ping\peon.ps1` and `config.json`.

## Install

Grab `PeonPingTray.exe` from the latest [GitHub Release](../../releases) (or build it),
then from the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File install.ps1
```

This copies the exe and `peonping-groups.conf` into `%LOCALAPPDATA%\PeonPingTray`,
adds a Startup-folder shortcut (launch at login), and starts it.

## Build from source

Requires the **.NET 10 SDK**:

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Output: `PeonPingTray\bin\publish\PeonPingTray.exe` (framework-dependent single-file,
~1–2 MB). The solution also opens and builds in Visual Studio / `dotnet build`.

## Customising the menu groups

The "Sound Pack" submenu groups packs by franchise via
`%LOCALAPPDATA%\PeonPingTray\peonping-groups.conf`. Each line maps a pack id or glob
to a group label (`tf2_* = Team Fortress 2`); first match wins, group order follows
first appearance, unmatched packs fall under **Other**. Edit and reopen the menu to
see changes. If the file is missing, packs are listed flat.

## Uninstall

```powershell
powershell -ExecutionPolicy Bypass -File uninstall.ps1
```

Removes the Startup shortcut, stops the tray, and deletes `%LOCALAPPDATA%\PeonPingTray`.
peon-ping itself is left untouched.

## Tests

```powershell
powershell -ExecutionPolicy Bypass -File tests\run-tests.ps1
```

Builds the exe and exercises the GUI-independent logic headlessly via the exe's
`--dump` / `--run-peon` / `--icon-selftest` modes. No third-party test framework.

## License & attribution

The **code** is MIT © hpsmcgregor (see [LICENSE](LICENSE)).

**Third-party, not ours:**

- **Tray icon** (`PeonPingTray/Resources/peon.png` / `.ico`) — the **Orc Peon from
  Warcraft, © Blizzard Entertainment**, included only to identify the tool. Swap in
  your own image if you prefer.
- **Sounds / voice packs** — belong to
  [peon-ping](https://github.com/PeonPing/peon-ping) and its packs (e.g. CC-BY-NC).
  **No audio is bundled here.**
- **peon-ping** — a separate project with its own license; a dependency, not included.
