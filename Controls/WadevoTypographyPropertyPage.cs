namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoTypographyPropertyPage : UserControl
{
    private readonly FlowLayoutPanel _layout = new();

    public event EventHandler<TypographyPropertyChangedEventArgs>? TypographyChanged;

    public WadevoTypographyPropertyPage()
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

        AddFontRow("Font Family", "Segoe UI");
        AddNumericRow("Title Size", 34, 8, 96);
        AddNumericRow("Header Size", 20, 8, 72);
        AddNumericRow("Body Size", 10, 8, 48);
        AddNumericRow("Line Height", 1.2m, 0.8m, 3.0m, 0.1m);
        AddToggleRow("Bold Title", true);
        AddToggleRow("Uppercase Title", false);
        AddToggleRow("Show Shadow Text", false);
    }

    private void AddFontRow(string label, string value)
    {
        Panel row = CreateRow();

        Label title = CreateLabel(label);

        WadevoComboBox comboBox = new()
        {
            Location = new Point(130, 5),
            Size = new Size(135, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = WadevoTheme.Colors.Panel,
            ForeColor = WadevoTheme.Colors.Text,
            Font = WadevoTheme.Fonts.Default
        };

        comboBox.Items.Add("Segoe UI");
        comboBox.Items.Add("Arial");
        comboBox.Items.Add("Verdana");
        comboBox.Items.Add("Tahoma");
        comboBox.Items.Add("Trebuchet MS");
        comboBox.Items.Add("Consolas");
        comboBox.SelectedItem = value;

        comboBox.SelectedIndexChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is string selected)
            {
                TypographyChanged?.Invoke(this, new TypographyPropertyChangedEventArgs(label, selected));
            }
        };

        row.Controls.Add(title);
        row.Controls.Add(comboBox);
        _layout.Controls.Add(row);
    }

    private void AddNumericRow(string label, decimal value, decimal min, decimal max, decimal increment = 1)
    {
        Panel row = CreateRow();

        Label title = CreateLabel(label);

        NumericUpDown numericBox = new()
        {
            Location = new Point(130, 5),
            Size = new Size(135, 30),
            Minimum = min,
            Maximum = max,
            Increment = increment,
            DecimalPlaces = increment < 1 ? 1 : 0,
            Value = value,
            BackColor = WadevoTheme.Colors.Panel,
            ForeColor = WadevoTheme.Colors.Text,
            Font = WadevoTheme.Fonts.Default
        };

        numericBox.ValueChanged += (_, _) =>
        {
            TypographyChanged?.Invoke(this, new TypographyPropertyChangedEventArgs(label, numericBox.Value));
        };

        row.Controls.Add(title);
        row.Controls.Add(numericBox);
        _layout.Controls.Add(row);
    }

    private void AddToggleRow(string label, bool value)
    {
        Panel row = CreateRow();

        Label title = CreateLabel(label);

        WadevoCheckBox checkBox = new()
        {
            Location = new Point(130, 9),
            Size = new Size(135, 24),
            Checked = value,
            Text = value ? "On" : "Off",
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = WadevoTheme.Colors.Card,
            Font = WadevoTheme.Fonts.Default,
            Cursor = Cursors.Hand
        };

        checkBox.CheckedChanged += (_, _) =>
        {
            checkBox.Text = checkBox.Checked ? "On" : "Off";
            TypographyChanged?.Invoke(this, new TypographyPropertyChangedEventArgs(label, checkBox.Checked));
        };

        row.Controls.Add(title);
        row.Controls.Add(checkBox);
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
            Size = new Size(125, 22),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = WadevoTheme.Colors.Card
        };
    }
}

public sealed class TypographyPropertyChangedEventArgs : EventArgs
{
    public TypographyPropertyChangedEventArgs(string propertyName, object value)
    {
        PropertyName = propertyName;
        Value = value;
    }

    public string PropertyName { get; }

    public object Value { get; }
}