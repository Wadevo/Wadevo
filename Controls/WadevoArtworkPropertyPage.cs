namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoArtworkPropertyPage : UserControl
{
    private readonly FlowLayoutPanel _layout = new();

    public event EventHandler<ArtworkPropertyChangedEventArgs>? ArtworkChanged;

    public WadevoArtworkPropertyPage()
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

        AddToggleRow("Show Artwork", true);
        AddNumericRow("Artwork Size", 96, 24, 300);
        AddNumericRow("Artwork Radius", 18, 0, 80);
        AddNumericRow("Artwork Opacity", 100, 0, 100);
        AddToggleRow("Artwork Shadow", true);
        AddToggleRow("Use Placeholder", true);
        AddTextRow("Placeholder Text", "♪");
    }

    private void AddTextRow(string label, string value)
    {
        Panel row = CreateRow();
        Label title = CreateLabel(label);

        TextBox textBox = new()
        {
            Location = new Point(135, 6),
            Size = new Size(130, 28),
            Text = value,
            BackColor = WadevoTheme.Colors.Panel,
            ForeColor = WadevoTheme.Colors.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = WadevoTheme.Fonts.Default
        };

        textBox.TextChanged += (_, _) =>
        {
            ArtworkChanged?.Invoke(this, new ArtworkPropertyChangedEventArgs(label, textBox.Text));
        };

        row.Controls.Add(title);
        row.Controls.Add(textBox);
        _layout.Controls.Add(row);
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
            ArtworkChanged?.Invoke(this, new ArtworkPropertyChangedEventArgs(label, numericBox.Value));
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
            Location = new Point(135, 9),
            Size = new Size(130, 24),
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
            ArtworkChanged?.Invoke(this, new ArtworkPropertyChangedEventArgs(label, checkBox.Checked));
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
            Size = new Size(130, 22),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = WadevoTheme.Colors.Card
        };
    }
}

public sealed class ArtworkPropertyChangedEventArgs : EventArgs
{
    public ArtworkPropertyChangedEventArgs(string propertyName, object value)
    {
        PropertyName = propertyName;
        Value = value;
    }

    public string PropertyName { get; }

    public object Value { get; }
}