namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public class WadevoToggle : Control
{
    private bool _isOn = true;

    public event EventHandler? IsOnChanged;

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn == value)
                return;

            _isOn = value;
            Invalidate();
            IsOnChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public WadevoToggle()
    {
        Size = new Size(76, 36);
        MinimumSize = new Size(60, 30);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        BackColor = WadevoTheme.Colors.BackgroundSoft;
        TabStop = true;
    }

    protected override void OnClick(EventArgs e)
    {
        IsOn = !IsOn;
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle track = new(1, 1, Width - 3, Height - 3);

        using GraphicsPath trackPath = CreateRoundedRectangle(track, track.Height);

        Color trackColor = IsOn
            ? WadevoTheme.Colors.Accent
            : WadevoTheme.Colors.BackgroundSoft;

        using SolidBrush trackBrush = new(trackColor);
        e.Graphics.FillPath(trackBrush, trackPath);

        int knobSize = Height - 10;
        int knobX = IsOn ? Width - knobSize - 6 : 6;
        Rectangle knob = new(knobX, 5, knobSize, knobSize);

        using SolidBrush knobBrush = new(Color.White);
        e.Graphics.FillEllipse(knobBrush, knob);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = Math.Min(radius, Math.Min(bounds.Width, bounds.Height));
        GraphicsPath path = new();

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}