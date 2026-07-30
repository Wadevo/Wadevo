namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public class WadevoSearchBox : UserControl
{
    private readonly TextBox _textBox = new();

    public event EventHandler? SearchTextChanged;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SearchText
    {
        get => _textBox.Text;
        set => _textBox.Text = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PlaceholderText
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value;
    }

    public WadevoSearchBox()
    {
        Height = 44;
        MinimumSize = new Size(120, 44);
        BackColor = Color.Transparent;
        Padding = new Padding(14, 0, 14, 0);

        _textBox.BorderStyle = BorderStyle.None;
        _textBox.BackColor = WadevoTheme.Colors.Card;
        _textBox.ForeColor = WadevoTheme.Colors.Text;
        _textBox.Font = WadevoTheme.Fonts.Default;
        _textBox.PlaceholderText = "Search commands...";
        _textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

        _textBox.TextChanged += (_, _) => SearchTextChanged?.Invoke(this, EventArgs.Empty);

        Controls.Add(_textBox);

        Resize += (_, _) => PositionTextBox();
        PositionTextBox();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using SolidBrush backgroundBrush = new(WadevoTheme.Colors.Card);
        using Pen borderPen = new(WadevoTheme.Colors.Border);

        Rectangle rect = new(0, 0, Width - 1, Height - 1);

        using GraphicsPath path = CreateRoundedRectangle(rect, 16);

        e.Graphics.FillPath(backgroundBrush, path);
        e.Graphics.DrawPath(borderPen, path);
    }

    private void PositionTextBox()
    {
        _textBox.Width = Math.Max(20, Width - 28);

        int textHeight = TextRenderer.MeasureText("Search commands...", _textBox.Font).Height;
        int y = Math.Max(0, (Height - textHeight) / 2);

        _textBox.Location = new Point(14, y);
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