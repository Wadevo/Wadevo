namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoColorPropertyPage : UserControl
{
    private readonly FlowLayoutPanel _layout = new();

    public event EventHandler<ColorPropertyChangedEventArgs>? ColorChanged;

    public WadevoColorPropertyPage()
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

        AddColorRow("Background", WadevoTheme.Colors.Background);
        AddColorRow("Background Soft", WadevoTheme.Colors.BackgroundSoft);
        AddColorRow("Panel", WadevoTheme.Colors.Panel);
        AddColorRow("Card", WadevoTheme.Colors.Card);
        AddColorRow("Accent", WadevoTheme.Colors.Accent);
        AddColorRow("Cyan", WadevoTheme.Colors.Cyan);
        AddColorRow("Purple", WadevoTheme.Colors.Purple);
        AddColorRow("Pink", WadevoTheme.Colors.Pink);
        AddColorRow("Orange", WadevoTheme.Colors.Orange);
        AddColorRow("Text", WadevoTheme.Colors.Text);
        AddColorRow("Text Secondary", WadevoTheme.Colors.TextSecondary);
        AddColorRow("Text Muted", WadevoTheme.Colors.TextMuted);
        AddColorRow("Success", WadevoTheme.Colors.Success);
        AddColorRow("Warning", WadevoTheme.Colors.Warning);
        AddColorRow("Error", WadevoTheme.Colors.Error);
        AddColorRow("Border", WadevoTheme.Colors.Border);
        AddColorRow("Border Glow", WadevoTheme.Colors.BorderGlow);
    }

    private void AddColorRow(string label, Color color)
    {
        Panel row = new()
        {
            Width = 270,
            Height = 42,
            BackColor = WadevoTheme.Colors.Card,
            Margin = new Padding(0, 0, 0, 8)
        };

        Label title = new()
        {
            Text = label,
            Location = new Point(0, 10),
            Size = new Size(145, 22),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = WadevoTheme.Colors.Card
        };

        Panel preview = new()
        {
            Location = new Point(152, 7),
            Size = new Size(34, 28),
            BackColor = color,
            Cursor = Cursors.Hand
        };

        Button button = new()
        {
            Text = "Edit",
            Location = new Point(198, 5),
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
            ColorChanged?.Invoke(this, new ColorPropertyChangedEventArgs(label, dialog.Color));
        }

        preview.Click += (_, _) => OpenPicker();
        button.Click += (_, _) => OpenPicker();

        row.Controls.Add(title);
        row.Controls.Add(preview);
        row.Controls.Add(button);

        _layout.Controls.Add(row);
    }
}

public sealed class ColorPropertyChangedEventArgs : EventArgs
{
    public ColorPropertyChangedEventArgs(string propertyName, Color color)
    {
        PropertyName = propertyName;
        Color = color;
    }

    public string PropertyName { get; }

    public Color Color { get; }
}