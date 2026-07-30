namespace Wadevo.Controls;

using System.Drawing.Drawing2D;
using Wadevo.Core;

/// <summary>
/// A drop-in replacement for the plain WinForms CheckBox. Inherits from CheckBox rather
/// than wrapping it, so Checked/CheckedChanged/Text and everything else callers already
/// rely on keep working unchanged - only the painting is overridden, to a bigger, themed
/// box instead of the tiny default system glyph.
///
/// Deliberately NOT using a transparent background. An earlier version tried
/// BackColor=Transparent + ControlStyles.SupportsTransparentBackColor, which in theory
/// should composite correctly over the parent - but in practice, inside these owner-drawn
/// popups it kept leaving stale pixels from whatever sibling control used to occupy that
/// screen space (visible as ghost text bleeding through behind the checkbox). Painting an
/// explicit opaque background first sidesteps that WinForms compositing quirk entirely
/// rather than continuing to fight it. FillColor defaults to the app's standard dark
/// background, which matches virtually every place this control is actually used; it's
/// exposed as a property for the rare container that isn't that color.
/// </summary>
public class WadevoCheckBox : CheckBox
{
    private const int BoxSize = 22;

    public Color FillColor { get; set; } = WadevoTheme.Colors.Background;

    public WadevoCheckBox()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = FillColor;
        ForeColor = WadevoTheme.Colors.Text;
        Font = WadevoTheme.Fonts.Default;
        Cursor = Cursors.Hand;
        MinimumSize = new Size(0, BoxSize + 6);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (SolidBrush backgroundBrush = new(FillColor))
        {
            g.FillRectangle(backgroundBrush, ClientRectangle);
        }

        int boxTop = Math.Max(0, (Height - BoxSize) / 2);
        Rectangle boxRect = new(0, boxTop, BoxSize, BoxSize);

        using GraphicsPath boxPath = CreateRoundedRectangle(boxRect, 6);

        Color fillColor = Checked ? WadevoTheme.Colors.Accent : WadevoTheme.Colors.BackgroundSoft;
        Color borderColor = Checked ? WadevoTheme.Colors.Accent : WadevoTheme.Colors.Border;

        using (SolidBrush fillBrush = new(fillColor))
        {
            g.FillPath(fillBrush, boxPath);
        }

        using (Pen borderPen = new(borderColor, 1.6f))
        {
            g.DrawPath(borderPen, boxPath);
        }

        if (Checked)
        {
            using Pen checkPen = new(WadevoTheme.Colors.Background, 2.6f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            PointF p1 = new(boxRect.Left + boxRect.Width * 0.22f, boxRect.Top + boxRect.Height * 0.55f);
            PointF p2 = new(boxRect.Left + boxRect.Width * 0.42f, boxRect.Top + boxRect.Height * 0.75f);
            PointF p3 = new(boxRect.Left + boxRect.Width * 0.8f, boxRect.Top + boxRect.Height * 0.28f);

            g.DrawLines(checkPen, new[] { p1, p2, p3 });
        }

        Rectangle textRect = new(BoxSize + 10, 0, Math.Max(0, Width - BoxSize - 10), Height);

        TextRenderer.DrawText(
            g,
            Text,
            Font,
            textRect,
            ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
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
