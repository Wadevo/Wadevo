namespace Wadevo.Controls;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public class WadevoTextBox : UserControl
{
    private readonly TextBox _textBox = new();
    private Color _accentColor = WadevoTheme.Colors.Accent;

    public event EventHandler? TextValueChanged;

    // The base UserControl already has its own KeyDown event, which won't fire while the
    // wrapped internal TextBox has focus (a distinct child control actually receives the
    // key events) - this passes those through explicitly for callers that need them, e.g.
    // an Enter-to-submit convenience on a search box.
    public event KeyEventHandler? InnerKeyDown;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Text
    {
        get => _textBox.Text;
        [param: AllowNull]
        set
        {
            _textBox.Text = value ?? string.Empty;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TextValue
    {
        get => _textBox.Text;
        set
        {
            _textBox.Text = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PlaceholderText
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value;
    }

    // Unlike an editable ComboBox (whose text area is a native Win32 edit control this
    // codebase can only reach via unreliable P/Invoke), a standalone TextBox genuinely
    // supports centered/right-aligned text as a normal, fully-supported WinForms property.
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HorizontalAlignment TextAlign
    {
        get => _textBox.TextAlign;
        set => _textBox.TextAlign = value;
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

    public WadevoTextBox()
    {
        Size = new Size(260, 44);
        MinimumSize = new Size(120, 38);
        BackColor = WadevoTheme.Colors.BackgroundSoft;
        DoubleBuffered = true;

        _textBox.BorderStyle = BorderStyle.None;
        _textBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _textBox.ForeColor = WadevoTheme.Colors.Text;
        _textBox.Font = WadevoTheme.Fonts.Default;
        _textBox.Location = new Point(16, 13);
        _textBox.Width = Width - 32;
        _textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

        _textBox.TextChanged += (_, _) =>
        {
            TextValueChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        };

        _textBox.GotFocus += (_, _) => Invalidate();
        _textBox.LostFocus += (_, _) => Invalidate();
        _textBox.KeyDown += (_, e) => InnerKeyDown?.Invoke(this, e);

        Controls.Add(_textBox);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        Height = Math.Max(Height, 38);
        _textBox.Location = new Point(16, (Height - _textBox.Height) / 2);
        _textBox.Width = Width - 32;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = CreateRoundedRectangle(bounds, 18);

        using SolidBrush fillBrush = new(WadevoTheme.Colors.BackgroundSoft);
        e.Graphics.FillPath(fillBrush, path);

        Color borderColor = _textBox.Focused
            ? Color.FromArgb(230, AccentColor)
            : Color.FromArgb(130, WadevoTheme.Colors.Border);

        using Pen borderPen = new(borderColor, 1);
        e.Graphics.DrawPath(borderPen, path);

        if (_textBox.Focused)
        {
            using Pen glowPen = new(Color.FromArgb(70, AccentColor), 3);
            e.Graphics.DrawPath(glowPen, path);
        }
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