namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public class WadevoSelectionCard : Control
{
    private bool _isSelected;

    public event EventHandler? CardClicked;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string IconText { get; set; } = "⭐";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TitleText { get; set; } = "Card Title";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DescriptionText { get; set; } = "Card description";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            Invalidate();
        }
    }

    public WadevoSelectionCard()
    {
        Size = new Size(210, 96);
        MinimumSize = new Size(160, 70);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        BackColor = WadevoTheme.Colors.Background;
    }

    protected override void OnClick(EventArgs e)
    {
        CardClicked?.Invoke(this, EventArgs.Empty);
        base.OnClick(e);
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

        Rectangle cardBounds = new(4, 4, Width - 9, Height - 9);

        using GraphicsPath cardPath = CreateRoundedRectangle(cardBounds, 16);

        Color fillColor = IsSelected
            ? Color.FromArgb(70, WadevoTheme.Colors.Accent)
            : WadevoTheme.Colors.BackgroundSoft;

        Color borderColor = IsSelected
            ? WadevoTheme.Colors.Accent
            : WadevoTheme.Colors.Purple;

        using SolidBrush fillBrush = new(fillColor);
        e.Graphics.FillPath(fillBrush, cardPath);

        if (IsSelected)
        {
            using Pen glowPen = new(Color.FromArgb(120, WadevoTheme.Colors.Accent), 6);
            e.Graphics.DrawPath(glowPen, cardPath);
        }

        using Pen borderPen = new(borderColor, IsSelected ? 3 : 1);
        e.Graphics.DrawPath(borderPen, cardPath);

        if (IsSelected)
        {
            Rectangle checkCircle = new(Width - 38, 8, 26, 26);

            using SolidBrush checkBrush = new(WadevoTheme.Colors.Accent);
            e.Graphics.FillEllipse(checkBrush, checkCircle);

            using Font checkFont = new(Font.FontFamily, 11, FontStyle.Bold);
            TextRenderer.DrawText(
                e.Graphics,
                "✓",
                checkFont,
                checkCircle,
                Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        using Font iconFont = new(Font.FontFamily, 22, FontStyle.Regular);
        using Font titleFont = new(Font.FontFamily, 10, FontStyle.Bold);
        using Font descriptionFont = new(Font.FontFamily, 9, FontStyle.Regular);

        Rectangle iconRect = new(18, 18, 42, 42);
        Rectangle titleRect = new(68, 20, Width - 95, 24);
        Rectangle descRect = new(68, 48, Width - 90, 40);

        TextRenderer.DrawText(
            e.Graphics,
            IconText,
            iconFont,
            iconRect,
            IsSelected ? WadevoTheme.Colors.Accent : WadevoTheme.Colors.Cyan,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        TextRenderer.DrawText(
            e.Graphics,
            TitleText,
            titleFont,
            titleRect,
            WadevoTheme.Colors.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

        TextRenderer.DrawText(
            e.Graphics,
            DescriptionText,
            descriptionFont,
            descRect,
            WadevoTheme.Colors.TextMuted,
            TextFormatFlags.Left | TextFormatFlags.WordBreak);
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