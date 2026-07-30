namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoDivider : Control
{
    public WadevoDivider()
    {
        Height = 12;
        Width = 260;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint,
            true);

        BackColor = WadevoTheme.Colors.Background;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode =
            System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using Pen pen = new(
            Color.FromArgb(55, WadevoTheme.Colors.TextMuted),
            1);

        int y = Height / 2;

        e.Graphics.DrawLine(
            pen,
            0,
            y,
            Width,
            y);
    }
}