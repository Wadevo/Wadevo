namespace Wadevo.Controls;

using System.ComponentModel;
using Wadevo.Core;

public sealed class WadevoColorPreview : Panel
{
    private readonly Label _label = new();
    private readonly Panel _colorPanel = new();

    private Color _selectedColor = WadevoTheme.Colors.Cyan;

    public event EventHandler? ColorChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string LabelText
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            _selectedColor = value;
            _colorPanel.BackColor = value;
            ColorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public WadevoColorPreview()
    {
        Size = new Size(260, 54);
        BackColor = Color.Transparent;

        _label.Location = new Point(0, 0);
        _label.Size = new Size(180, 20);
        _label.Font = WadevoTheme.Fonts.Default;
        _label.ForeColor = WadevoTheme.Colors.TextMuted;
        _label.BackColor = Color.Transparent;

        _colorPanel.Location = new Point(0, 26);
        _colorPanel.Size = new Size(80, 22);
        _colorPanel.BackColor = _selectedColor;
        _colorPanel.BorderStyle = BorderStyle.FixedSingle;
        _colorPanel.Cursor = Cursors.Hand;

        _colorPanel.Click += (_, _) =>
        {
            using ColorDialog dialog = new()
            {
                FullOpen = true,
                Color = _selectedColor
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SelectedColor = dialog.Color;
            }
        };

        Controls.Add(_label);
        Controls.Add(_colorPanel);
    }
}