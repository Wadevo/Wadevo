namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoSongQueueEditForm : WadevoDialogForm
{
    private readonly ComboBox _fontCombo = new();
    private readonly NumericUpDown _sizeBox = new();
    private readonly WadevoCheckBox _boldBox = new();
    private readonly Panel _colorSwatch = new();
    private readonly NumericUpDown _maxVisibleBox = new();

    private Color _selectedColor;

    public string SelectedFont => _fontCombo.SelectedItem?.ToString() ?? "Segoe UI";
    public float SelectedSize => (float)_sizeBox.Value;
    public bool SelectedBold => _boldBox.Checked;
    public Color SelectedColor => _selectedColor;
    public int SelectedMaxVisible => (int)_maxVisibleBox.Value;

    public WadevoSongQueueEditForm(
        string currentFont,
        float currentSize,
        bool currentBold,
        Color currentColor,
        int currentMaxVisible)
        : base("Style: Song Queue")
    {
        Size = new Size(400, 400);
        _selectedColor = currentColor;

        Label introLabel = new()
        {
            Text = "Shows your live song request queue - updates automatically, no need to reopen this.",
            Location = new Point(24, 16),
            Size = new Size(340, 34),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label fontLabel = new()
        {
            Text = "Font",
            Location = new Point(24, 60),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _fontCombo.Location = new Point(24, 82);
        _fontCombo.Size = new Size(200, 26);
        _fontCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        WadevoFontPickerHelper.PopulateFontCombo(_fontCombo, currentFont);
        WadevoFontPickerHelper.WireUploadOption(_fontCombo, this);

        Label sizeLabel = new()
        {
            Text = "Size",
            Location = new Point(236, 60),
            Size = new Size(60, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _sizeBox.Minimum = 8;
        _sizeBox.Maximum = 72;
        _sizeBox.Value = (decimal)Math.Clamp(currentSize, 8, 72);
        _sizeBox.Location = new Point(236, 82);
        _sizeBox.Size = new Size(60, 26);

        _boldBox.Text = "Bold";
        _boldBox.Checked = currentBold;
        _boldBox.Location = new Point(304, 80);
        _boldBox.Size = new Size(70, 28);
        _boldBox.ForeColor = WadevoTheme.Colors.Text;

        Label colorLabel = new()
        {
            Text = "Color",
            Location = new Point(24, 124),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _colorSwatch.Location = new Point(24, 146);
        _colorSwatch.Size = new Size(48, 30);
        _colorSwatch.BackColor = currentColor;
        _colorSwatch.Cursor = Cursors.Hand;
        _colorSwatch.Click += (_, _) => PickColor();

        WadevoButton chooseColorButton = new()
        {
            ButtonText = "Choose Color",
            Location = new Point(84, 146),
            Size = new Size(140, 30),
            AccentColor = WadevoTheme.Colors.Cyan
        };
        chooseColorButton.ButtonClicked += (_, _) => PickColor();

        Label maxVisibleLabel = new()
        {
            Text = "Max songs to show at once",
            Location = new Point(24, 196),
            Size = new Size(300, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _maxVisibleBox.Minimum = 1;
        _maxVisibleBox.Maximum = 25;
        _maxVisibleBox.Value = Math.Clamp(currentMaxVisible, 1, 25);
        _maxVisibleBox.Location = new Point(24, 218);
        _maxVisibleBox.Size = new Size(80, 26);

        WadevoButton saveButton = new()
        {
            ButtonText = "Save",
            Location = new Point(192, 300),
            Size = new Size(84, 38),
            AccentColor = WadevoTheme.Colors.Success
        };

        WadevoButton cancelButton = new()
        {
            ButtonText = "Cancel",
            Location = new Point(292, 300),
            Size = new Size(84, 38),
            AccentColor = WadevoTheme.Colors.TextMuted
        };

        saveButton.ButtonClicked += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        cancelButton.ButtonClicked += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        ContentPanel.Controls.Add(introLabel);
        ContentPanel.Controls.Add(fontLabel);
        ContentPanel.Controls.Add(_fontCombo);
        ContentPanel.Controls.Add(sizeLabel);
        ContentPanel.Controls.Add(_sizeBox);
        ContentPanel.Controls.Add(_boldBox);
        ContentPanel.Controls.Add(colorLabel);
        ContentPanel.Controls.Add(_colorSwatch);
        ContentPanel.Controls.Add(chooseColorButton);
        ContentPanel.Controls.Add(maxVisibleLabel);
        ContentPanel.Controls.Add(_maxVisibleBox);
        ContentPanel.Controls.Add(saveButton);
        ContentPanel.Controls.Add(cancelButton);
    }

    private void PickColor()
    {
        using ColorDialog dialog = new() { Color = _selectedColor, FullOpen = true };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectedColor = dialog.Color;
            _colorSwatch.BackColor = _selectedColor;
        }
    }
}
