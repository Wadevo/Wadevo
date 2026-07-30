namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoPaddingPropertyPage : UserControl
{
    private readonly FlowLayoutPanel _layout = new();

    public event EventHandler<PaddingPropertyChangedEventArgs>? PaddingValueChanged;

    public WadevoPaddingPropertyPage()
    {
        Dock = DockStyle.Fill;
        BackColor = WadevoTheme.Colors.Card;
        Padding = new Padding(14);

        _layout.Dock = DockStyle.Fill;
        _layout.FlowDirection = FlowDirection.TopDown;
        _layout.WrapContents = false;
        _layout.AutoScroll = true;
        _layout.BackColor = WadevoTheme.Colors.Card;

        Controls.Add(_layout);

        AddNumericRow("Padding All", WadevoTheme.Sizes.PaddingMedium, 0, 100);
        AddNumericRow("Padding Top", WadevoTheme.Sizes.PaddingMedium, 0, 100);
        AddNumericRow("Padding Right", WadevoTheme.Sizes.PaddingMedium, 0, 100);
        AddNumericRow("Padding Bottom", WadevoTheme.Sizes.PaddingMedium, 0, 100);
        AddNumericRow("Padding Left", WadevoTheme.Sizes.PaddingMedium, 0, 100);
        AddNumericRow("Gap", 12, 0, 80);
        AddNumericRow("Artwork Gap", 14, 0, 80);
    }

    private void AddNumericRow(string label, decimal value, decimal min, decimal max)
    {
        Panel row = CreateRow();
        Label title = CreateLabel(label);

        NumericUpDown numericBox = new()
        {
            Location = new Point(135, 5),
            Size = new Size(130, 30),
            Minimum = min,
            Maximum = max,
            Value = value,
            BackColor = WadevoTheme.Colors.Panel,
            ForeColor = WadevoTheme.Colors.Text,
            Font = WadevoTheme.Fonts.Default
        };

        numericBox.ValueChanged += (_, _) =>
        {
            PaddingValueChanged?.Invoke(this, new PaddingPropertyChangedEventArgs(label, numericBox.Value));
        };

        row.Controls.Add(title);
        row.Controls.Add(numericBox);
        _layout.Controls.Add(row);
    }

    private static Panel CreateRow()
    {
        return new Panel
        {
            Width = 280,
            Height = 42,
            BackColor = WadevoTheme.Colors.Card,
            Margin = new Padding(0, 0, 0, 8)
        };
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            Location = new Point(0, 10),
            Size = new Size(130, 22),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = WadevoTheme.Colors.Card
        };
    }
}

public sealed class PaddingPropertyChangedEventArgs : EventArgs
{
    public PaddingPropertyChangedEventArgs(string propertyName, decimal value)
    {
        PropertyName = propertyName;
        Value = value;
    }

    public string PropertyName { get; }

    public decimal Value { get; }
}