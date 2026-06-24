using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PeonPingTray.Core;

public static class IconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool DestroyIcon(IntPtr hIcon);

    static Bitmap? _base;

    static Bitmap BaseImage()
    {
        if (_base is not null) return _base;
        Assembly asm = Assembly.GetExecutingAssembly();
        using Stream? s = asm.GetManifestResourceStream("PeonPingTray.Resources.peon.png");
        if (s is null) throw new InvalidOperationException("Embedded peon.png resource not found.");
        _base = new Bitmap(s);
        return _base;
    }

    public static Icon Create(string state)
    {
        Color dot = state == "ON" ? Color.FromArgb(52, 199, 89)
                  : state == "OFF" ? Color.FromArgb(255, 59, 48)
                  : Color.FromArgb(142, 142, 147);

        using var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            g.DrawImage(BaseImage(), new Rectangle(0, 0, 16, 16));
            int d = 8;
            var rect = new Rectangle(16 - d, 16 - d, d - 1, d - 1);
            using (var b = new SolidBrush(dot)) g.FillEllipse(b, rect);
            using (var pen = new Pen(Color.White, 1f)) g.DrawEllipse(pen, rect);
        }

        IntPtr hicon = bmp.GetHicon();
        try { return (Icon)Icon.FromHandle(hicon).Clone(); }
        finally { DestroyIcon(hicon); }
    }
}
