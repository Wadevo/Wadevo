namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

public sealed class ChatOverlaySettingsForm : WadevoDialogForm
{
    private readonly ChatOverlaySettingsStore _store = new();

    private readonly WadevoCheckBox _showBlazeCheckBox = new();
    private readonly WadevoCheckBox _showTwitchCheckBox = new();
    private readonly WadevoCheckBox _showPlatformLabelCheckBox = new();
    private readonly NumericUpDown _maxVisibleUpDown = new();
    private readonly NumericUpDown _fontSizeUpDown = new();
    private readonly TextBox _fontFamilyTextBox = new();
    private readonly TextBox _textColorTextBox = new();
    private readonly TextBox _bubbleColorTextBox = new();
    private readonly NumericUpDown _bubbleOpacityUpDown = new();
    private readonly WadevoComboBox _alignmentComboBox = new();

    public ChatOverlaySettingsForm() : base("💬 Combined Chat Overlay Settings")
    {
        Size = new Size(480, 560);
        ContentPanel.AutoScroll = true;

        ChatOverlaySettings settings = _store.Load();

        Label platformsHeader = SectionHeader("Platforms shown", 16);

        _showBlazeCheckBox.Text = "🔥  Show Blaze chat";
        _showBlazeCheckBox.Location = new Point(24, 46);
        _showBlazeCheckBox.Size = new Size(400, 24);
        _showBlazeCheckBox.Checked = settings.ShowBlaze;

        _showTwitchCheckBox.Text = "🟣  Show Twitch chat";
        _showTwitchCheckBox.Location = new Point(24, 72);
        _showTwitchCheckBox.Size = new Size(400, 24);
        _showTwitchCheckBox.Checked = settings.ShowTwitch;

        _showPlatformLabelCheckBox.Text = "Show platform name next to each message";
        _showPlatformLabelCheckBox.Location = new Point(24, 98);
        _showPlatformLabelCheckBox.Size = new Size(400, 24);
        _showPlatformLabelCheckBox.Checked = settings.ShowPlatformLabel;

        Label appearanceHeader = SectionHeader("Appearance", 136);

        Label maxVisibleLabel = FieldLabel("Messages visible at once", 24, 168);
        _maxVisibleUpDown.Location = new Point(260, 166);
        _maxVisibleUpDown.Size = new Size(160, 28);
        _maxVisibleUpDown.Minimum = 1;
        _maxVisibleUpDown.Maximum = 40;
        _maxVisibleUpDown.Value = Math.Clamp(settings.MaxVisibleMessages, 1, 40);

        Label fontFamilyLabel = FieldLabel("Font", 24, 200);
        _fontFamilyTextBox.Location = new Point(260, 198);
        _fontFamilyTextBox.Size = new Size(160, 28);
        _fontFamilyTextBox.Text = settings.FontFamily;

        Label fontSizeLabel = FieldLabel("Font size (px)", 24, 232);
        _fontSizeUpDown.Location = new Point(260, 230);
        _fontSizeUpDown.Size = new Size(160, 28);
        _fontSizeUpDown.Minimum = 10;
        _fontSizeUpDown.Maximum = 32;
        _fontSizeUpDown.Value = Math.Clamp(settings.FontSizePx, 10, 32);

        Label textColorLabel = FieldLabel("Text color (hex)", 24, 264);
        _textColorTextBox.Location = new Point(260, 262);
        _textColorTextBox.Size = new Size(160, 28);
        _textColorTextBox.Text = settings.TextColorHex;

        Label bubbleColorLabel = FieldLabel("Bubble background (hex)", 24, 296);
        _bubbleColorTextBox.Location = new Point(260, 294);
        _bubbleColorTextBox.Size = new Size(160, 28);
        _bubbleColorTextBox.Text = settings.BubbleBackgroundHex;

        Label bubbleOpacityLabel = FieldLabel("Bubble opacity (%)", 24, 328);
        _bubbleOpacityUpDown.Location = new Point(260, 326);
        _bubbleOpacityUpDown.Size = new Size(160, 28);
        _bubbleOpacityUpDown.Minimum = 10;
        _bubbleOpacityUpDown.Maximum = 100;
        _bubbleOpacityUpDown.Value = Math.Clamp(settings.BubbleOpacityPercent, 10, 100);

        Label alignmentLabel = FieldLabel("Messages align to", 24, 360);
        _alignmentComboBox.Location = new Point(260, 358);
        _alignmentComboBox.Size = new Size(160, 28);
        _alignmentComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _alignmentComboBox.Items.Add("Left");
        _alignmentComboBox.Items.Add("Right");
        _alignmentComboBox.SelectedIndex = settings.Alignment == "right" ? 1 : 0;

        WadevoButton saveButton = new()
        {
            ButtonText = "Save",
            Location = new Point(24, 410),
            Size = new Size(120, 40),
            AccentColor = WadevoTheme.Colors.Accent
        };
        saveButton.ButtonClicked += (_, _) => SaveAndClose();

        WadevoButton cancelButton = new()
        {
            ButtonText = "Cancel",
            Location = new Point(154, 410),
            Size = new Size(120, 40),
            AccentColor = WadevoTheme.Colors.TextMuted
        };
        cancelButton.ButtonClicked += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Label hint = new()
        {
            Text = "Changes apply the next time the /chat overlay page refreshes in OBS " +
                   "(right-click the Browser Source > Refresh, if it doesn't pick it up automatically).",
            Location = new Point(24, 460),
            Size = new Size(420, 40),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        ContentPanel.Controls.Add(platformsHeader);
        ContentPanel.Controls.Add(_showBlazeCheckBox);
        ContentPanel.Controls.Add(_showTwitchCheckBox);
        ContentPanel.Controls.Add(_showPlatformLabelCheckBox);
        ContentPanel.Controls.Add(appearanceHeader);
        ContentPanel.Controls.Add(maxVisibleLabel);
        ContentPanel.Controls.Add(_maxVisibleUpDown);
        ContentPanel.Controls.Add(fontFamilyLabel);
        ContentPanel.Controls.Add(_fontFamilyTextBox);
        ContentPanel.Controls.Add(fontSizeLabel);
        ContentPanel.Controls.Add(_fontSizeUpDown);
        ContentPanel.Controls.Add(textColorLabel);
        ContentPanel.Controls.Add(_textColorTextBox);
        ContentPanel.Controls.Add(bubbleColorLabel);
        ContentPanel.Controls.Add(_bubbleColorTextBox);
        ContentPanel.Controls.Add(bubbleOpacityLabel);
        ContentPanel.Controls.Add(_bubbleOpacityUpDown);
        ContentPanel.Controls.Add(alignmentLabel);
        ContentPanel.Controls.Add(_alignmentComboBox);
        ContentPanel.Controls.Add(saveButton);
        ContentPanel.Controls.Add(cancelButton);
        ContentPanel.Controls.Add(hint);
    }

    private static Label SectionHeader(string text, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(24, y),
            Size = new Size(400, 24),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };
    }

    private static Label FieldLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y + 4),
            Size = new Size(220, 22),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };
    }

    private void SaveAndClose()
    {
        ChatOverlaySettings settings = new()
        {
            ShowBlaze = _showBlazeCheckBox.Checked,
            ShowTwitch = _showTwitchCheckBox.Checked,
            ShowPlatformLabel = _showPlatformLabelCheckBox.Checked,
            MaxVisibleMessages = (int)_maxVisibleUpDown.Value,
            FontFamily = string.IsNullOrWhiteSpace(_fontFamilyTextBox.Text) ? "Segoe UI" : _fontFamilyTextBox.Text.Trim(),
            FontSizePx = (int)_fontSizeUpDown.Value,
            TextColorHex = NormalizeHex(_textColorTextBox.Text, "#F2F7FB"),
            BubbleBackgroundHex = NormalizeHex(_bubbleColorTextBox.Text, "#060A10"),
            BubbleOpacityPercent = (int)_bubbleOpacityUpDown.Value,
            Alignment = _alignmentComboBox.SelectedIndex == 1 ? "right" : "left"
        };

        _store.Save(settings);

        DialogResult = DialogResult.OK;
        Close();
    }

    private static string NormalizeHex(string input, string fallback)
    {
        string trimmed = input.Trim();

        if (!trimmed.StartsWith('#'))
        {
            trimmed = "#" + trimmed;
        }

        try
        {
            _ = ColorTranslator.FromHtml(trimmed);
            return trimmed;
        }
        catch
        {
            return fallback;
        }
    }
}
