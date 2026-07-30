namespace Wadevo.Controls;

using System.ComponentModel;
using Wadevo.Core;

public sealed class WadevoLabeledNumericBox : Panel
{
    private readonly Label _label = new();
    private readonly NumericUpDown _numeric = new();

    public event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string LabelText
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Value
    {
        get => _numeric.Value;
        set => _numeric.Value = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Minimum
    {
        get => _numeric.Minimum;
        set => _numeric.Minimum = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Maximum
    {
        get => _numeric.Maximum;
        set => _numeric.Maximum = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public NumericUpDown InnerNumericBox => _numeric;

    public WadevoLabeledNumericBox()
    {
        Size = new Size(170, 58);
        BackColor = Color.Transparent;

        _label.Location = new Point(0, 0);
        _label.Size = new Size(170, 20);
        _label.Font = WadevoTheme.Fonts.Default;
        _label.ForeColor = WadevoTheme.Colors.TextMuted;
        _label.BackColor = Color.Transparent;

        _numeric.Location = new Point(0, 24);
        _numeric.Size = new Size(170, 28);
        _numeric.Minimum = 0;
        _numeric.Maximum = 60000;
        _numeric.Increment = 100;
        _numeric.Font = WadevoTheme.Fonts.Default;
        _numeric.ForeColor = WadevoTheme.Colors.Text;
        _numeric.BackColor = WadevoTheme.Colors.Panel;
        _numeric.BorderStyle = BorderStyle.FixedSingle;

        _numeric.ValueChanged += (_, _) => ValueChanged?.Invoke(this, EventArgs.Empty);

        Controls.Add(_label);
        Controls.Add(_numeric);
    }
}