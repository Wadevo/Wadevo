namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public class WadevoOverlayListItem : Control
{
    private bool _selected;
    private bool _hover;

    public event EventHandler? ItemClicked;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string OverlayName { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string OverlayType { get; set; } = "Song ID";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SavedAtText { get; set; } = "";

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

    public WadevoOverlayListItem()
    {
        Size = new Size(196, 82);
        MinimumSize = new Size(160, 82);
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
            _selected ? WadevoTheme.Colors.Accent : Color.FromArgb(130, WadevoTheme.Colors.Cyan),
            _selected ? 2 : 1);

        e.Graphics.DrawPath(border, path);

        DrawIcon(e.Graphics);
        DrawTextContent(e.Graphics);
    }

    private void DrawIcon(Graphics graphics)
    {
        Rectangle iconBubble = new(12, 17, 36, 36);

        using SolidBrush bubbleBrush = new(Color.FromArgb(35, WadevoTheme.Colors.Cyan));
        graphics.FillEllipse(bubbleBrush, iconBubble);

        using Pen bubbleBorder = new(Color.FromArgb(120, WadevoTheme.Colors.Cyan), 1);
        graphics.DrawEllipse(bubbleBorder, iconBubble);

        Rectangle iconArea = Rectangle.Inflate(iconBubble, -8, -8);
        WadevoIconRenderer.Draw(graphics, WadevoIconKind.NowPlaying, iconArea, WadevoTheme.Colors.Text);
    }

    private void DrawTextContent(Graphics graphics)
    {
        using Font titleFont = new(Font.FontFamily, 9, FontStyle.Bold);
        using Font typeFont = new(Font.FontFamily, 8, FontStyle.Bold);
        using Font dateFont = new(Font.FontFamily, 7, FontStyle.Bold);

        TextRenderer.DrawText(
            graphics,
            OverlayName,
            titleFont,
            new Rectangle(58, 12, Width - 74, 22),
            WadevoTheme.Colors.Text,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            graphics,
            OverlayType,
            typeFont,
            new Rectangle(58, 34, Width - 74, 18),
            WadevoTheme.Colors.Cyan,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        TextRenderer.DrawText(
            graphics,
            SavedAtText,
            dateFont,
            new Rectangle(58, 54, Width - 74, 18),
            _selected ? WadevoTheme.Colors.Accent : WadevoTheme.Colors.TextMuted,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
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
