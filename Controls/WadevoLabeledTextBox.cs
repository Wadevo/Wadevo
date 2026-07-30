namespace Wadevo.Controls;

using System.ComponentModel;
using Wadevo.Core;

public sealed class WadevoLabeledTextBox : Panel
{
    private readonly Label _label = new();
    private readonly TextBox _textBox = new();

    public event EventHandler? TextValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string LabelText
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TextValue
    {
        get => _textBox.Text;
        set => _textBox.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox InnerTextBox => _textBox;

    public WadevoLabeledTextBox()
    {
        Size = new Size(260, 58);
        BackColor = Color.Transparent;

        _label.Location = new Point(0, 0);
        _label.Size = new Size(260, 20);
        _label.Font = WadevoTheme.Fonts.Default;
        _label.ForeColor = WadevoTheme.Colors.TextMuted;
        _label.BackColor = Color.Transparent;

        _textBox.Location = new Point(0, 24);
        _textBox.Size = new Size(260, 28);
        _textBox.Font = WadevoTheme.Fonts.Default;
        _textBox.ForeColor = WadevoTheme.Colors.Text;
        _textBox.BackColor = WadevoTheme.Colors.Panel;
        _textBox.BorderStyle = BorderStyle.FixedSingle;

        _textBox.TextChanged += (_, _) => TextValueChanged?.Invoke(this, EventArgs.Empty);

        Controls.Add(_label);
        Controls.Add(_textBox);
    }
}