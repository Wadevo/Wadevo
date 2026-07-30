namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public class WadevoGlassCard : Panel
{
    private Color _accentColor = WadevoTheme.Colors.Accent;
    private bool _showGlow = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowGlow
    {
        get => _showGlow;
        set { _showGlow = value; Invalidate(); }
    }

    public WadevoGlassCard()
    {
        BackColor = WadevoTheme.Colors.Card;
        ForeColor = WadevoTheme.Colors.Text;
        DoubleBuffered = true;
        ResizeRedraw = true;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);

        Padding = new Padding(24);
        Margin = new Padding(0);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Previously left empty to avoid a black-box artifact behind transparent-background
        // child Labels. That trade was wrong - suppressing the erase step entirely meant this
        // control's region was never cleared before drawing, so stale content from elsewhere
        // on screen could remain visible underneath. Explicitly clearing to the same color
        // OnPaint already uses fixes both problems: no black box, and no stale content
        // bleeding through from anywhere else.
        Color clearColor = Parent?.BackColor == Color.Transparent
            ? WadevoTheme.Colors.Background
            : Parent?.BackColor ?? WadevoTheme.Colors.Background;

        e.Graphics.Clear(clearColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // During layout, a card can briefly be sized down to almost nothing before its
        // final Dock/Anchor size is applied. There's nothing meaningful to draw at that
        // size, and the glow-line math below breaks down for very small rectangles.
        if (Width <= 10 || Height <= 10)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        Color clearColor = Parent?.BackColor == Color.Transparent
            ? WadevoTheme.Colors.Background
            : Parent?.BackColor ?? WadevoTheme.Colors.Background;

        e.Graphics.Clear(clearColor);

        Rectangle cardRect = ShowGlow
            ? new Rectangle(2, 2, Width - 5, Height - 6)
            : new Rectangle(0, 0, Width - 1, Height - 1);

        int radius = ShowGlow
            ? WadevoTheme.Sizes.BorderRadius
            : 18;

        if (ShowGlow)
        {
            Rectangle outerGlowRect = new(3, 4, Width - 7, Height - 9);

            using GraphicsPath outerGlowPath = RoundedRect(outerGlowRect, radius + 4);
            using Pen outerGlowPen = new(Color.FromArgb(45, AccentColor), 5);
            e.Graphics.DrawPath(outerGlowPen, outerGlowPath);
        }

        using GraphicsPath cardPath = RoundedRect(cardRect, radius);

        using LinearGradientBrush background = new(
            cardRect,
            WadevoTheme.Colors.Card,
            WadevoTheme.Colors.BackgroundSoft,
            LinearGradientMode.ForwardDiagonal);

        e.Graphics.FillPath(background, cardPath);

        using Pen borderPen = new(Color.FromArgb(185, AccentColor), ShowGlow ? 1 : 2);
        e.Graphics.DrawPath(borderPen, cardPath);

        // Only draw the top glow line when there's actually enough width for it to make sense -
        // otherwise the rectangle it's drawn into can collapse to zero width and crash.
        if (ShowGlow && cardRect.Width > 60)
        {
            Rectangle glowRect = new(cardRect.X + 24, cardRect.Y, cardRect.Width - 48, 1);

            using LinearGradientBrush glowBrush = new(
                glowRect,
                Color.FromArgb(0, AccentColor),
                Color.FromArgb(230, AccentColor),
                LinearGradientMode.Horizontal);

            using Pen glowPen = new(glowBrush, 3)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            e.Graphics.DrawLine(
                glowPen,
                cardRect.X + 26,
                cardRect.Y + 1,
                cardRect.Right - 26,
                cardRect.Y + 1);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        GraphicsPath path = new();
        int diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}