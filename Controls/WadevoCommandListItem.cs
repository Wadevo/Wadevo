namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public class WadevoCommandListItem : Control
{
    private bool _selected;
    private bool _hover;

    public event EventHandler? ItemClicked;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CommandName { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Trigger { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CommandType { get; set; } = "Chat Message";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EnabledCommand { get; set; } = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            Invalidate();
        }
    }

    public WadevoCommandListItem()
    {
        Size = new Size(225, 82);
        MinimumSize = new Size(200, 82);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        BackColor = WadevoTheme.Colors.Background;
    }

    protected override void OnClick(EventArgs e)
    {
        ItemClicked?.Invoke(this, EventArgs.Empty);
        base.OnClick(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
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

        Rectangle bounds = new(4, 4, Width - 9, Height - 9);
        using GraphicsPath path = Rounded(bounds, 16);

        Color fill = _selected
            ? Color.FromArgb(78, WadevoTheme.Colors.Accent)
            : _hover
                ? WadevoTheme.Colors.BackgroundSoft
                : WadevoTheme.Colors.Background;

        if (_selected)
        {
            using Pen glow = new(Color.FromArgb(85, WadevoTheme.Colors.Accent), 6);
            e.Graphics.DrawPath(glow, path);
        }

        using SolidBrush brush = new(fill);
        e.Graphics.FillPath(brush, path);

        using Pen border = new(
            _selected ? WadevoTheme.Colors.Accent : Color.FromArgb(130, WadevoTheme.Colors.Purple),
            _selected ? 2 : 1);

        e.Graphics.DrawPath(border, path);

        DrawIcon(e.Graphics);
        DrawTextContent(e.Graphics);
        DrawStatusBadge(e.Graphics);
    }

    private void DrawIcon(Graphics graphics)
    {
        string icon = CommandType switch
        {
            "Chat Message" => "💬",
            "GIF / Image" => "🖼",
            "Video Clip" => "🎬",
            "Sound Effect" => "🔊",
            "Multi Action" => "🎉",
            "Alert" => "🚨",
            "Change OBS Scene" => "🎥",
            _ => "⭐"
        };

        Rectangle iconBubble = new(12, 17, 36, 36);

        using SolidBrush bubbleBrush = new(Color.FromArgb(35, WadevoTheme.Colors.Cyan));
        graphics.FillEllipse(bubbleBrush, iconBubble);

        using Pen bubbleBorder = new(Color.FromArgb(120, WadevoTheme.Colors.Cyan), 1);
        graphics.DrawEllipse(bubbleBorder, iconBubble);

        using Font iconFont = new(Font.FontFamily, 14);

        TextRenderer.DrawText(
            graphics,
            icon,
            iconFont,
            iconBubble,
            WadevoTheme.Colors.Text,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding);
    }

    private void DrawTextContent(Graphics graphics)
    {
        using Font titleFont = new(Font.FontFamily, 9, FontStyle.Bold);
        using Font triggerFont = new(Font.FontFamily, 8, FontStyle.Bold);
        using Font typeFont = new(Font.FontFamily, 7, FontStyle.Bold);

        TextRenderer.DrawText(
            graphics,
            CommandName,
            titleFont,
            new Rectangle(58, 12, Width - 108, 22),
            WadevoTheme.Colors.Text,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            graphics,
            Trigger,
            triggerFont,
            new Rectangle(58, 34, Width - 108, 18),
            WadevoTheme.Colors.Cyan,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            graphics,
            CommandType,
            typeFont,
            new Rectangle(58, 54, Width - 108, 18),
            _selected ? WadevoTheme.Colors.Accent : WadevoTheme.Colors.TextMuted,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }

    private void DrawStatusBadge(Graphics graphics)
    {
        Rectangle badge = new(Width - 40, 27, 18, 18);

        Color badgeColor = EnabledCommand
            ? WadevoTheme.Colors.Accent
            : WadevoTheme.Colors.TextMuted;

        using SolidBrush badgeBrush = new(Color.FromArgb(45, badgeColor));
        graphics.FillEllipse(badgeBrush, badge);

        using Pen badgePen = new(Color.FromArgb(180, badgeColor), 1);
        graphics.DrawEllipse(badgePen, badge);

        Rectangle dot = new(badge.X + 6, badge.Y + 6, 6, 6);

        using SolidBrush dotBrush = new(badgeColor);
        graphics.FillEllipse(dotBrush, dot);
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        GraphicsPath path = new();
        int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));

        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        return path;
    }
}