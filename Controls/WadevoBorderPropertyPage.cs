namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoBorderPropertyPage : UserControl
{
    private readonly FlowLayoutPanel _layout = new();

    public event EventHandler<BorderPropertyChangedEventArgs>? BorderChanged;

    public WadevoBorderPropertyPage()
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

        AddToggleRow("Show Border", true);
        AddNumericRow("Border Width", 2, 0, 20);
        AddNumericRow("Border Radius", WadevoTheme.Sizes.BorderRadius, 0, 60);
        AddColorRow("Border Color", WadevoTheme.Colors.Border);
        AddColorRow("Glow Color", WadevoTheme.Colors.BorderGlow);
        AddToggleRow("Enable Glow", true);
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
            BorderChanged?.Invoke(this, new BorderPropertyChangedEventArgs(label, numericBox.Value));
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
            BorderChanged?.Invoke(this, new BorderPropertyChangedEventArgs(label, checkBox.Checked));
        };

        row.Controls.Add(title);
        row.Controls.Add(checkBox);
        _layout.Controls.Add(row);
    }

    private void AddColorRow(string label, Color value)
    {
        Panel row = CreateRow();
        Label title = CreateLabel(label);

        Panel preview = new()
        {
            Location = new Point(135, 7),
            Size = new Size(34, 28),
            BackColor = value,
            Cursor = Cursors.Hand
        };

        Button button = new()
        {
            Text = "Edit",
            Location = new Point(180, 5),
            Size = new Size(58, 30),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = WadevoTheme.Colors.CardHover,
            ForeColor = WadevoTheme.Colors.Text,
            Font = WadevoTheme.Fonts.Default
        };

        button.FlatAppearance.BorderColor = WadevoTheme.Colors.Border;
        button.FlatAppearance.MouseOverBackColor = WadevoTheme.Colors.Panel;

        void OpenPicker()
        {
            using ColorDialog dialog = new()
            {
                Color = preview.BackColor,
                FullOpen = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            preview.BackColor = dialog.Color;
            BorderChanged?.Invoke(this, new BorderPropertyChangedEventArgs(label, dialog.Color));
        }

        preview.Click += (_, _) => OpenPicker();
        button.Click += (_, _) => OpenPicker();

        row.Controls.Add(title);
        row.Controls.Add(preview);
        row.Controls.Add(button);
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

public sealed class BorderPropertyChangedEventArgs : EventArgs
{
    public BorderPropertyChangedEventArgs(string propertyName, object value)
    {
        PropertyName = propertyName;
        Value = value;
    }

    public string PropertyName { get; }

    public object Value { get; }
}