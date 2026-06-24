using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using PeonPingTray.Core;

namespace PeonPingTray;

public sealed class TrayApplicationContext : ApplicationContext
{
    readonly string _hookDir;
    readonly NotifyIcon _icon;
    readonly ContextMenuStrip _menu;
    readonly Control _marshal;       // owns a handle on the UI thread for cross-thread Invoke
    FileSystemWatcher? _watcher;
    string? _lastState;

    public TrayApplicationContext()
    {
        _hookDir = Paths.DefaultHookDir();

        _marshal = new Control();
        _ = _marshal.Handle;          // force handle creation now (UI thread)

        _menu = new ContextMenuStrip();
        _menu.Opening += MenuOpening;

        _icon = new NotifyIcon { Visible = true, ContextMenuStrip = _menu };
        _icon.MouseClick += IconMouseClick;

        UpdateIcon();
        SetupWatcher();
    }

    void IconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        // Show the same context menu on left-click.
        MethodInfo? m = typeof(NotifyIcon).GetMethod("ShowContextMenu",
            BindingFlags.Instance | BindingFlags.NonPublic);
        m?.Invoke(_icon, null);
    }

    void SetupWatcher()
    {
        try
        {
            string dir = Path.GetDirectoryName(Paths.ConfigJson(_hookDir)) ?? "";
            if (!Directory.Exists(dir)) return;
            _watcher = new FileSystemWatcher(dir, "config.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };
            FileSystemEventHandler h = (_, _) => OnConfigChanged();
            _watcher.Changed += h;
            _watcher.Created += h;
            _watcher.Renamed += (_, _) => OnConfigChanged();
            _watcher.EnableRaisingEvents = true;
        }
        catch { /* watcher is best-effort */ }
    }

    void OnConfigChanged()
    {
        try
        {
            if (_marshal.InvokeRequired) _marshal.BeginInvoke((Action)UpdateIcon);
            else UpdateIcon();
        }
        catch { }
    }

    void UpdateIcon()
    {
        PeonConfig cfg = PeonConfig.Read(_hookDir);
        string state = cfg.State;

        if (state != _lastState)
        {
            Icon? old = _icon.Icon;
            _icon.Icon = IconFactory.Create(state);
            old?.Dispose();
            _lastState = state;
        }

        string label = state switch { "ON" => "ON", "OFF" => "OFF (muted)", _ => "unknown" };
        string pack = cfg.DefaultPack ?? "";
        string text = "Peon-Ping: " + label + (pack.Length > 0 ? " - " + pack : "");
        if (text.Length > 63) text = text.Substring(0, 63);
        _icon.Text = text;
    }

    void MenuOpening(object? sender, CancelEventArgs e)
    {
        e.Cancel = false;
        BuildMenu();
    }

    void BuildMenu()
    {
        _menu.Items.Clear();
        PeonConfig cfg = PeonConfig.Read(_hookDir);
        string state = cfg.State;

        var header = new ToolStripMenuItem(state switch
        {
            "ON" => "Peon-Ping is ON",
            "OFF" => "Peon-Ping is OFF",
            _ => "Peon-Ping: state unknown"
        }) { Enabled = false };
        _menu.Items.Add(header);

        if (state == "OFF")
            _menu.Items.Add(Item("Unmute", (_, _) => { PeonCli.Run(_hookDir, "resume"); UpdateIcon(); }));
        else
            _menu.Items.Add(Item("Mute", (_, _) => { PeonCli.Run(_hookDir, "pause"); UpdateIcon(); }));

        _menu.Items.Add(new ToolStripSeparator());

        List<PackInfo> packs = PackCatalog.Discover(_hookDir);
        GroupRules rules = GroupRules.Load(Paths.GroupsConf());
        string? current = cfg.DefaultPack;

        string currentDisplay = current ?? "(none)";
        foreach (PackInfo p in packs) if (p.Id == current) { currentDisplay = p.DisplayName; break; }

        _menu.Items.Add(new ToolStripMenuItem("Sound Pack: " + currentDisplay) { Enabled = false });

        double vol = cfg.Volume;
        var ids = new List<string>();
        foreach (PackInfo p in packs) ids.Add(p.Id);
        List<string> ordered = rules.OrderedGroups(ids);

        if (ordered.Count <= 1)
        {
            foreach (PackInfo p in packs) _menu.Items.Add(BuildPackItem(p, current, vol));
        }
        else
        {
            foreach (string grp in ordered)
            {
                var groupItem = new ToolStripMenuItem(grp);
                foreach (PackInfo p in packs)
                    if (rules.GroupFor(p.Id) == grp) groupItem.DropDownItems.Add(BuildPackItem(p, current, vol));
                _menu.Items.Add(groupItem);
            }
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(Item("Refresh", (_, _) => UpdateIcon()));
        _menu.Items.Add(Item("Exit", (_, _) => ExitApp()));
    }

    ToolStripMenuItem BuildPackItem(PackInfo p, string? current, double volume)
    {
        var item = new ToolStripMenuItem(p.DisplayName) { Checked = p.Id == current };
        string id = p.Id;
        var use = new ToolStripMenuItem("Use this pack");
        use.Click += (_, _) => { PeonCli.Run(_hookDir, "packs", "use", id); UpdateIcon(); };
        item.DropDownItems.Add(use);

        if (!string.IsNullOrEmpty(p.PreviewSound))
        {
            string sound = p.PreviewSound;
            var prev = new ToolStripMenuItem("▶ Preview");
            prev.Click += (_, _) => PeonCli.PlaySound(_hookDir, sound, volume);
            item.DropDownItems.Add(prev);
        }
        return item;
    }

    static ToolStripMenuItem Item(string text, EventHandler onClick)
    {
        var i = new ToolStripMenuItem(text);
        i.Click += onClick;
        return i;
    }

    void ExitApp()
    {
        try { _icon.Visible = false; } catch { }
        if (_watcher is not null) { try { _watcher.EnableRaisingEvents = false; _watcher.Dispose(); } catch { } }
        try { _icon.Dispose(); } catch { }
        try { _marshal.Dispose(); } catch { }
        ExitThread();
    }
}
