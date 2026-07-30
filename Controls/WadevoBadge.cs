namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public class WadevoBadge : Control
{
    private string _badgeText = "Building: Chat Message";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string BadgeText
    {
        get => _badgeText;
        set
        {
            _badgeText = value;
            Invalidate();
        }
    }

    public WadevoBadge()
    {
        Size = new Size(190, 38);
        MinimumSize = new Size(120, 30);
        DoubleBuffered = true;
        BackColor = WadevoTheme.Colors.Background;
    }

    private Color FindOpaqueBackColor()
    {
        Control? current = Parent;

        while (current is not null)
        {
            if (current.BackColor != Color.Transparent)
            {
                return current.BackColor;
            }

            current = current.Parent;
        }

        return WadevoTheme.Colors.Background;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(FindOpaqueBackColor());

        Rectangle bounds = new(2, 2, Width - 5, Height - 5);

        using GraphicsPath path = CreateRoundedRectangle(bounds, 14);

        using SolidBrush fillBrush = new(Color.FromArgb(35, WadevoTheme.Colors.Purple));
        e.Graphics.FillPath(fillBrush, path);

        using Pen borderPen = new(WadevoTheme.Colors.Purple, 2);
        e.Graphics.DrawPath(borderPen, path);

        using Font textFont = new(Font.FontFamily, 9, FontStyle.Bold);

        TextRenderer.DrawText(
            e.Graphics,
            BadgeText,
            textFont,
            bounds,
            WadevoTheme.Colors.Cyan,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        GraphicsPath path = new();

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}