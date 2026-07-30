namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public class WadevoButton : Control
{
    private bool _isHovered;
    private bool _isPressed;
    private Color _accentColor = WadevoTheme.Colors.Accent;

    public event EventHandler? ButtonClicked;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ButtonText
    {
        get => Text;
        set { Text = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
    }

    public WadevoButton()
    {
        Text = "Button";
        Size = new Size(150, 44);
        MinimumSize = new Size(90, 36);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        BackColor = WadevoTheme.Colors.Background;
        Font = WadevoTheme.Fonts.Bold;
        TabStop = true;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        ButtonClicked?.Invoke(this, EventArgs.Empty);
        base.OnClick(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isPressed = true;
            Focus();
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _isPressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }

        base.OnKeyDown(e);
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

        Rectangle bounds = new(3, 3, Width - 7, Height - 7);

        if (_isPressed)
            bounds.Offset(1, 1);

        using GraphicsPath path = CreateRoundedRectangle(bounds, 17);

        if (_isHovered || Focused)
        {
            using Pen glowPen = new(Color.FromArgb(105, AccentColor), 5);
            e.Graphics.DrawPath(glowPen, path);
        }

        Color fillColor = _isPressed
            ? Color.FromArgb(8, 16, 13)
            : _isHovered
                ? Color.FromArgb(24, 45, 34)
                : Color.FromArgb(12, 24, 19);

        using SolidBrush fillBrush = new(fillColor);
        e.Graphics.FillPath(fillBrush, path);

        Color borderColor = _isHovered || Focused
            ? Color.FromArgb(255, AccentColor)
            : Color.FromArgb(175, AccentColor);

        using Pen borderPen = new(borderColor, 2);
        e.Graphics.DrawPath(borderPen, path);

        Rectangle textBounds = bounds;
        if (_isPressed)
            textBounds.Offset(1, 1);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textBounds,
            Color.White,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding |
            TextFormatFlags.EndEllipsis);
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