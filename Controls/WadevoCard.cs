namespace Wadevo.Controls;

using Wadevo.Core;
using System.ComponentModel;
using System.Drawing.Drawing2D;

public class WadevoCard : Panel
{
    private int _cornerRadius = WadevoTheme.Sizes.BorderRadius;
    private Color _borderColor = WadevoTheme.Colors.Border;
    private Color _accentColor = WadevoTheme.Colors.Accent;
    private bool _showAccent = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            _accentColor = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowAccent
    {
        get => _showAccent;
        set
        {
            _showAccent = value;
            Invalidate();
        }
    }

    public WadevoCard()
    {
        BackColor = WadevoTheme.Colors.Card;
        ForeColor = WadevoTheme.Colors.Text;
        Font = WadevoTheme.Fonts.Normal;
        DoubleBuffered = true;
        ResizeRedraw = true;
        Padding = new Padding(WadevoTheme.Sizes.PaddingMedium);
        Margin = new Padding(0);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle rect = new(0, 0, Width - 1, Height - 1);

        using GraphicsPath path = GetRoundedRectangle(rect, CornerRadius);
        using SolidBrush backgroundBrush = new(BackColor);
        using Pen borderPen = new(BorderColor, 1);

        Region = new Region(path);

        e.Graphics.FillPath(backgroundBrush, path);
        e.Graphics.DrawPath(borderPen, path);

        if (ShowAccent)
        {
            using Pen accentPen = new(AccentColor, 4)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            e.Graphics.DrawLine(accentPen, 1, CornerRadius, 1, Height - CornerRadius);
        }
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Invalidate();
    }

    private static GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
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