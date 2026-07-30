namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoGoalEditForm : WadevoDialogForm
{
    private static readonly string[] Metrics = { "Followers", "Subscriptions", "GiftSubs", "Votes" };
    private static readonly (string Value, string Label)[] Platforms =
    {
        ("All", "All Platforms (combined)"),
        ("Blaze", "Blaze only"),
        ("Twitch", "Twitch only")
    };

    private readonly ComboBox _metricCombo = new();
    private readonly ComboBox _platformCombo = new();
    private readonly NumericUpDown _targetBox = new();
    private readonly Panel _fillSwatch = new();
    private readonly Panel _trackSwatch = new();
    private readonly ComboBox _fontCombo = new();
    private readonly NumericUpDown _sizeBox = new();
    private readonly WadevoCheckBox _boldBox = new();
    private readonly Panel _textColorSwatch = new();

    private Color _fillColor;
    private Color _trackColor;
    private Color _textColor;

    public string SelectedMetric => _metricCombo.SelectedItem?.ToString() ?? "Followers";
    public string SelectedPlatform => Platforms[Math.Max(0, _platformCombo.SelectedIndex)].Value;
    public int SelectedTarget => (int)_targetBox.Value;
    public Color SelectedFillColor => _fillColor;
    public Color SelectedTrackColor => _trackColor;
    public string SelectedFont => _fontCombo.SelectedItem?.ToString() ?? "Segoe UI";
    public float SelectedSize => (float)_sizeBox.Value;
    public bool SelectedBold => _boldBox.Checked;
    public Color SelectedTextColor => _textColor;

    public WadevoGoalEditForm(
        string currentMetric,
        string currentPlatform,
        int currentTarget,
        Color currentFillColor,
        Color currentTrackColor,
        string currentFont,
        float currentSize,
        bool currentBold,
        Color currentTextColor)
        : base("Style: Goal Bar")
    {
        Size = new Size(400, 572);
        _fillColor = currentFillColor;
        _trackColor = currentTrackColor;
        _textColor = currentTextColor;

        Label introLabel = new()
        {
            Text = "Tracks progress toward a goal for today's stream - resets automatically at the start of each new day.",
            Location = new Point(24, 16),
            Size = new Size(340, 34),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label metricLabel = new()
        {
            Text = "Track",
            Location = new Point(24, 58),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _metricCombo.Location = new Point(24, 80);
        _metricCombo.Size = new Size(160, 26);
        _metricCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _metricCombo.Items.AddRange(Metrics);
        int metricIndex = Array.IndexOf(Metrics, currentMetric);
        _metricCombo.SelectedIndex = metricIndex >= 0 ? metricIndex : 0;

        Label targetLabel = new()
        {
            Text = "Goal (today)",
            Location = new Point(200, 58),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _targetBox.Minimum = 1;
        _targetBox.Maximum = 1_000_000;
        _targetBox.Value = Math.Clamp(currentTarget, 1, 1_000_000);
        _targetBox.Location = new Point(200, 80);
        _targetBox.Size = new Size(140, 26);

        Label platformLabel = new()
        {
            Text = "Count from",
            Location = new Point(24, 122),
            Size = new Size(340, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _platformCombo.Location = new Point(24, 144);
        _platformCombo.Size = new Size(316, 26);
        _platformCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _platformCombo.Items.AddRange(Platforms.Select(p => p.Label).ToArray());
        int platformIndex = Array.FindIndex(Platforms, p => p.Value == currentPlatform);
        _platformCombo.SelectedIndex = platformIndex >= 0 ? platformIndex : 0;

        Label fillLabel = new()
        {
            Text = "Fill Color",
            Location = new Point(24, 174),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _fillSwatch.Location = new Point(24, 196);
        _fillSwatch.Size = new Size(160, 30);
        _fillSwatch.BackColor = _fillColor;
        _fillSwatch.Cursor = Cursors.Hand;
        _fillSwatch.Click += (_, _) => PickColor(_fillSwatch, c => _fillColor = c);

        Label trackLabel = new()
        {
            Text = "Track Color",
            Location = new Point(200, 174),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _trackSwatch.Location = new Point(200, 196);
        _trackSwatch.Size = new Size(140, 30);
        _trackSwatch.BackColor = _trackColor;
        _trackSwatch.Cursor = Cursors.Hand;
        _trackSwatch.Click += (_, _) => PickColor(_trackSwatch, c => _trackColor = c);

        Label labelStyleHeader = new()
        {
            Text = "Label text (e.g. \"42 / 100 Followers\")",
            Location = new Point(24, 246),
            Size = new Size(340, 20),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label fontLabel = new()
        {
            Text = "Font",
            Location = new Point(24, 274),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _fontCombo.Location = new Point(24, 296);
        _fontCombo.Size = new Size(200, 26);
        _fontCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        WadevoFontPickerHelper.PopulateFontCombo(_fontCombo, currentFont);
        WadevoFontPickerHelper.WireUploadOption(_fontCombo, this);

        Label sizeLabel = new()
        {
            Text = "Size",
            Location = new Point(236, 274),
            Size = new Size(60, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _sizeBox.Minimum = 8;
        _sizeBox.Maximum = 72;
        _sizeBox.Value = (decimal)Math.Clamp(currentSize, 8, 72);
        _sizeBox.Location = new Point(236, 296);
        _sizeBox.Size = new Size(60, 26);

        _boldBox.Text = "Bold";
        _boldBox.Checked = currentBold;
        _boldBox.Location = new Point(304, 294);
        _boldBox.Size = new Size(70, 28);
        _boldBox.ForeColor = WadevoTheme.Colors.Text;

        Label textColorLabel = new()
        {
            Text = "Text Color",
            Location = new Point(24, 338),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _textColorSwatch.Location = new Point(24, 360);
        _textColorSwatch.Size = new Size(160, 30);
        _textColorSwatch.BackColor = _textColor;
        _textColorSwatch.Cursor = Cursors.Hand;
        _textColorSwatch.Click += (_, _) => PickColor(_textColorSwatch, c => _textColor = c);

        WadevoButton saveButton = new()
        {
            ButtonText = "Save",
            Location = new Point(192, 492),
            Size = new Size(84, 38),
            AccentColor = WadevoTheme.Colors.Success
        };

        WadevoButton cancelButton = new()
        {
            ButtonText = "Cancel",
            Location = new Point(292, 492),
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
        ContentPanel.Controls.Add(metricLabel);
        ContentPanel.Controls.Add(_metricCombo);
        ContentPanel.Controls.Add(platformLabel);
        ContentPanel.Controls.Add(_platformCombo);
        ContentPanel.Controls.Add(targetLabel);
        ContentPanel.Controls.Add(_targetBox);
        ContentPanel.Controls.Add(fillLabel);
        ContentPanel.Controls.Add(_fillSwatch);
        ContentPanel.Controls.Add(trackLabel);
        ContentPanel.Controls.Add(_trackSwatch);
        ContentPanel.Controls.Add(labelStyleHeader);
        ContentPanel.Controls.Add(fontLabel);
        ContentPanel.Controls.Add(_fontCombo);
        ContentPanel.Controls.Add(sizeLabel);
        ContentPanel.Controls.Add(_sizeBox);
        ContentPanel.Controls.Add(_boldBox);
        ContentPanel.Controls.Add(textColorLabel);
        ContentPanel.Controls.Add(_textColorSwatch);
        ContentPanel.Controls.Add(saveButton);
        ContentPanel.Controls.Add(cancelButton);
    }

    private void PickColor(Panel swatch, Action<Color> apply)
    {
        using ColorDialog dialog = new() { Color = swatch.BackColor, FullOpen = true };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            apply(dialog.Color);
            swatch.BackColor = dialog.Color;
        }
    }
}
