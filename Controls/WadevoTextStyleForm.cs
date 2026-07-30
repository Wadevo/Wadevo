namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoTextStyleForm : WadevoDialogForm
{

    private readonly TextBox? _textBox;
    private readonly WadevoComboBox _fontCombo = new();
    private readonly NumericUpDown _sizeBox = new();
    private readonly WadevoCheckBox _boldBox = new();
    private readonly Panel _colorSwatch = new();

    private Color _selectedColor;

    public string TextValue => _textBox?.Text ?? "";
    public string SelectedFont => _fontCombo.SelectedItem?.ToString() ?? "Segoe UI";
    public float SelectedSize => (float)_sizeBox.Value;
    public bool SelectedBold => _boldBox.Checked;
    public Color SelectedColor => _selectedColor;

    public WadevoTextStyleForm(
        string title,
        bool showTextField,
        string currentText,
        string currentFont,
        float currentSize,
        bool currentBold,
        Color currentColor)
        : base(title)
    {
        Size = new Size(400, showTextField ? 466 : 354);

        _selectedColor = currentColor;

        int y = 20;

        if (showTextField)
        {
            Label textLabel = new()
            {
                Text = "Text",
                Location = new Point(24, y),
                Size = new Size(340, 20),
                Font = WadevoTheme.Fonts.Small,
                ForeColor = WadevoTheme.Colors.TextMuted,
                BackColor = Color.Transparent
            };

            _textBox = new TextBox
            {
                Location = new Point(24, y + 22),
                Size = new Size(298, 26),
                Text = currentText,
                Font = WadevoTheme.Fonts.Default,
                BackColor = WadevoTheme.Colors.BackgroundSoft,
                ForeColor = WadevoTheme.Colors.Text,
                BorderStyle = BorderStyle.FixedSingle
            };

            WadevoButton emoteButton = new()
            {
                ButtonText = "😀",
                Location = new Point(328, y + 20),
                Size = new Size(36, 30),
                AccentColor = WadevoTheme.Colors.Accent
            };

            emoteButton.ButtonClicked += (_, _) => EmotePickerPopup.ShowFor(emoteButton, _textBox);

            Label variablesHintLabel = new()
            {
                Text = "If this is on an Alert: {username} {message} {viewerCount} {giftCount} {voteAmount}",
                Location = new Point(24, y + 50),
                Size = new Size(340, 32),
                Font = WadevoTheme.Fonts.Small,
                ForeColor = WadevoTheme.Colors.Cyan,
                BackColor = Color.Transparent
            };

            ContentPanel.Controls.Add(textLabel);
            ContentPanel.Controls.Add(_textBox);
            ContentPanel.Controls.Add(emoteButton);
            ContentPanel.Controls.Add(variablesHintLabel);

            y += 92;
        }

        Label fontLabel = new()
        {
            Text = "Font",
            Location = new Point(24, y),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _fontCombo.Location = new Point(24, y + 22);
        _fontCombo.Size = new Size(200, 26);
        _fontCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        WadevoFontPickerHelper.PopulateFontCombo(_fontCombo, currentFont);
        WadevoFontPickerHelper.WireUploadOption(_fontCombo, this);

        Label sizeLabel = new()
        {
            Text = "Size",
            Location = new Point(236, y),
            Size = new Size(60, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _sizeBox.Location = new Point(236, y + 22);
        _sizeBox.Size = new Size(60, 26);
        _sizeBox.Minimum = 8;
        _sizeBox.Maximum = 72;
        _sizeBox.Value = (decimal)Math.Clamp(currentSize, 8, 72);

        _boldBox.Text = "Bold";
        _boldBox.Location = new Point(308, y + 20);
        _boldBox.Size = new Size(70, 28);
        _boldBox.ForeColor = WadevoTheme.Colors.Text;
        _boldBox.Checked = currentBold;

        y += 68;

        Label colorLabel = new()
        {
            Text = "Color",
            Location = new Point(24, y),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _colorSwatch.Location = new Point(24, y + 22);
        _colorSwatch.Size = new Size(60, 30);
        _colorSwatch.BackColor = _selectedColor;
        _colorSwatch.Cursor = Cursors.Hand;
        _colorSwatch.BorderStyle = BorderStyle.FixedSingle;

        WadevoButton pickColorButton = new()
        {
            ButtonText = "Choose Color",
            Location = new Point(96, y + 20),
            Size = new Size(140, 34),
            AccentColor = WadevoTheme.Colors.Accent
        };

        pickColorButton.ButtonClicked += (_, _) => PickColor();
        _colorSwatch.Click += (_, _) => PickColor();

        y += 70;

        WadevoButton saveButton = new()
        {
            ButtonText = "Save",
            Location = new Point(192, y),
            Size = new Size(84, 38),
            AccentColor = WadevoTheme.Colors.Success
        };

        WadevoButton cancelButton = new()
        {
            ButtonText = "Cancel",
            Location = new Point(292, y),
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

        ContentPanel.Controls.Add(fontLabel);
        ContentPanel.Controls.Add(_fontCombo);
        ContentPanel.Controls.Add(sizeLabel);
        ContentPanel.Controls.Add(_sizeBox);
        ContentPanel.Controls.Add(_boldBox);
        ContentPanel.Controls.Add(colorLabel);
        ContentPanel.Controls.Add(_colorSwatch);
        ContentPanel.Controls.Add(pickColorButton);
        ContentPanel.Controls.Add(saveButton);
        ContentPanel.Controls.Add(cancelButton);
    }

    private void PickColor()
    {
        using ColorDialog dialog = new()
        {
            Color = _selectedColor,
            FullOpen = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectedColor = dialog.Color;
            _colorSwatch.BackColor = _selectedColor;
        }
    }
}
