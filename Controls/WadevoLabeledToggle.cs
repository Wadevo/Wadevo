namespace Wadevo.Controls;

using System.ComponentModel;
using Wadevo.Core;

public sealed class WadevoLabeledToggle : Panel
{
    private readonly Label _label = new();
    private readonly WadevoCheckBox _checkBox = new();

    public event EventHandler? CheckedChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string LabelText
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => _checkBox.Checked;
        set => _checkBox.Checked = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public WadevoCheckBox InnerCheckBox => _checkBox;

    public WadevoLabeledToggle()
    {
        Size = new Size(240, 40);
        BackColor = Color.Transparent;

        _label.Location = new Point(0, 10);
        _label.Size = new Size(170, 20);
        _label.Font = WadevoTheme.Fonts.Default;
        _label.ForeColor = WadevoTheme.Colors.Text;
        _label.BackColor = Color.Transparent;

        _checkBox.Location = new Point(182, 8);
        _checkBox.Size = new Size(24, 24);
        _checkBox.BackColor = Color.Transparent;
        _checkBox.ForeColor = WadevoTheme.Colors.Text;

        _checkBox.CheckedChanged += (_, _) =>
        {
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        };

        Controls.Add(_label);
        Controls.Add(_checkBox);
    }
}