namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoCountdownEditForm : WadevoDialogForm
{
    private readonly DateTimePicker _dateBox = new();
    private readonly DateTimePicker _timeBox = new();
    private readonly TextBox _labelBox = new();
    private readonly TextBox _completedTextBox = new();
    private readonly ComboBox _fontCombo = new();
    private readonly NumericUpDown _sizeBox = new();
    private readonly WadevoCheckBox _boldBox = new();
    private readonly Panel _colorSwatch = new();

    private Color _selectedColor;

    public DateTime SelectedTargetUtc { get; private set; }

    public string SelectedLabel => _labelBox.Text;

    public string SelectedCompletedText => _completedTextBox.Text;

    public string SelectedFont => _fontCombo.SelectedItem?.ToString() ?? "Segoe UI";

    public float SelectedSize => (float)_sizeBox.Value;

    public bool SelectedBold => _boldBox.Checked;

    public Color SelectedColor => _selectedColor;

    public WadevoCountdownEditForm(
        DateTime currentTargetUtc,
        string currentLabel,
        string currentCompletedText,
        string currentFont,
        float currentSize,
        bool currentBold,
        Color currentColor)
        : base("Edit Countdown")
    {
        Size = new Size(400, 460);
        _selectedColor = currentColor;

        DateTime currentTargetLocal = currentTargetUtc.ToLocalTime();

        int y = 20;

        Label dateLabel = new()
        {
            Text = "Counts down to (your local time)",
            Location = new Point(24, y),
            Size = new Size(340, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _dateBox.Location = new Point(24, y + 22);
        _dateBox.Size = new Size(160, 26);
        _dateBox.Format = DateTimePickerFormat.Short;
        _dateBox.Value = currentTargetLocal.Date;

        _timeBox.Location = new Point(196, y + 22);
        _timeBox.Size = new Size(168, 26);
        _timeBox.Format = DateTimePickerFormat.Time;
        _timeBox.ShowUpDown = true;
        _timeBox.Value = currentTargetLocal;

        y += 60;

        Label labelLabel = new()
        {
            Text = "Label (shown above the timer, optional)",
            Location = new Point(24, y),
            Size = new Size(340, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _labelBox.Location = new Point(24, y + 22);
        _labelBox.Size = new Size(340, 26);
        _labelBox.Text = currentLabel;
        _labelBox.PlaceholderText = "e.g. Stream starts in";
        _labelBox.Font = WadevoTheme.Fonts.Default;
        _labelBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _labelBox.ForeColor = WadevoTheme.Colors.Text;
        _labelBox.BorderStyle = BorderStyle.FixedSingle;

        y += 60;

        Label completedLabel = new()
        {
            Text = "Message shown once the countdown reaches zero",
            Location = new Point(24, y),
            Size = new Size(340, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _completedTextBox.Location = new Point(24, y + 22);
        _completedTextBox.Size = new Size(340, 26);
        _completedTextBox.Text = currentCompletedText;
        _completedTextBox.PlaceholderText = "e.g. 🎉 It's time!";
        _completedTextBox.Font = WadevoTheme.Fonts.Default;
        _completedTextBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _completedTextBox.ForeColor = WadevoTheme.Colors.Text;
        _completedTextBox.BorderStyle = BorderStyle.FixedSingle;

        y += 70;

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
        _fontCombo.Size = new Size(160, 26);
        _fontCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        WadevoFontPickerHelper.PopulateFontCombo(_fontCombo, currentFont);
        WadevoFontPickerHelper.WireUploadOption(_fontCombo, this);

        Label sizeLabel = new()
        {
            Text = "Size",
            Location = new Point(196, y),
            Size = new Size(60, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _sizeBox.Minimum = 8;
        _sizeBox.Maximum = 72;
        _sizeBox.Value = (decimal)Math.Clamp(currentSize, 8, 72);
        _sizeBox.Location = new Point(196, y + 22);
        _sizeBox.Size = new Size(60, 26);

        _boldBox.Text = "Bold";
        _boldBox.Checked = currentBold;
        _boldBox.Location = new Point(264, y + 20);
        _boldBox.Size = new Size(70, 28);
        _boldBox.ForeColor = WadevoTheme.Colors.Text;

        y += 60;

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
        _colorSwatch.Size = new Size(48, 30);
        _colorSwatch.BackColor = currentColor;
        _colorSwatch.Cursor = Cursors.Hand;
        _colorSwatch.Click += (_, _) => PickColor();

        WadevoButton chooseColorButton = new()
        {
            ButtonText = "Choose Color",
            Location = new Point(84, y + 22),
            Size = new Size(140, 30),
            AccentColor = WadevoTheme.Colors.Cyan
        };
        chooseColorButton.ButtonClicked += (_, _) => PickColor();

        y += 62;

        WadevoButton quickHourButton = new()
        {
            ButtonText = "+1 hour",
            Location = new Point(24, y),
            Size = new Size(90, 32),
            AccentColor = WadevoTheme.Colors.Cyan
        };

        WadevoButton quickDayButton = new()
        {
            ButtonText = "+1 day",
            Location = new Point(120, y),
            Size = new Size(90, 32),
            AccentColor = WadevoTheme.Colors.Cyan
        };

        WadevoButton quickWeekButton = new()
        {
            ButtonText = "+1 week",
            Location = new Point(216, y),
            Size = new Size(90, 32),
            AccentColor = WadevoTheme.Colors.Cyan
        };

        quickHourButton.ButtonClicked += (_, _) => ApplyQuickOffset(TimeSpan.FromHours(1));
        quickDayButton.ButtonClicked += (_, _) => ApplyQuickOffset(TimeSpan.FromDays(1));
        quickWeekButton.ButtonClicked += (_, _) => ApplyQuickOffset(TimeSpan.FromDays(7));

        y += 50;

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
            DateTime combinedLocal = _dateBox.Value.Date + _timeBox.Value.TimeOfDay;
            SelectedTargetUtc = DateTime.SpecifyKind(combinedLocal, DateTimeKind.Local).ToUniversalTime();

            DialogResult = DialogResult.OK;
            Close();
        };

        cancelButton.ButtonClicked += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        ContentPanel.Controls.Add(dateLabel);
        ContentPanel.Controls.Add(_dateBox);
        ContentPanel.Controls.Add(_timeBox);
        ContentPanel.Controls.Add(labelLabel);
        ContentPanel.Controls.Add(_labelBox);
        ContentPanel.Controls.Add(completedLabel);
        ContentPanel.Controls.Add(_completedTextBox);
        ContentPanel.Controls.Add(fontLabel);
        ContentPanel.Controls.Add(_fontCombo);
        ContentPanel.Controls.Add(sizeLabel);
        ContentPanel.Controls.Add(_sizeBox);
        ContentPanel.Controls.Add(_boldBox);
        ContentPanel.Controls.Add(colorLabel);
        ContentPanel.Controls.Add(_colorSwatch);
        ContentPanel.Controls.Add(chooseColorButton);
        ContentPanel.Controls.Add(quickHourButton);
        ContentPanel.Controls.Add(quickDayButton);
        ContentPanel.Controls.Add(quickWeekButton);
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

    private void ApplyQuickOffset(TimeSpan offset)
    {
        DateTime target = DateTime.Now + offset;
        _dateBox.Value = target.Date;
        _timeBox.Value = target;
    }
}
