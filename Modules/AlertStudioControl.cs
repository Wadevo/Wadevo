namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services.Platforms;

/// <summary>
/// Trigger config AND appearance design, on one screen, with no page navigation between
/// them - the Overlay Designer canvas is embedded directly here rather than being a
/// separate page you jump to and back from. Configure what fires the alert at the top,
/// design what it looks like right below it, save once.
/// </summary>
public sealed class AlertStudioControl : UserControl
{
    private static readonly (string Trigger, string Description)[] KnownTriggers =
        new (string Trigger, string Description)[] { ("", "Manual / test only") }
            .Concat(
                PlatformRegistry.Implemented.SelectMany(platform =>
                    platform.AlertEvents.Select(alertEvent =>
                        (platform.GetTriggerName(alertEvent.EventName), $"{platform.Name}: {GetDropdownDescription(alertEvent)}"))))
            .ToArray();

    private static string GetDropdownDescription(PlatformAlertEvent alertEvent)
    {
        // Matches the previous hand-written phrasing (e.g. "New follower" rather than
        // just "Follower") - kept as its own small mapping since the dropdown's fuller
        // sentence style differs from the shorter filter-chip label used elsewhere.
        return alertEvent.EventName switch
        {
            "follow" => "New follower",
            "raid" => "Raid received",
            "subscribe" => "New subscription",
            "gift" => "Gift subs",
            "vote" => "Vote / poll",
            "vip" => "New VIP",
            "cheer" => "Cheer / bits",
            "online" => "Stream went live",
            "offline" => "Stream went offline",
            "chat" => "Any chat message",
            _ => alertEvent.Label
        };
    }

    private readonly WadevoLabeledTextBox _nameBox = new();
    private readonly WadevoComboBox _triggerCombo = new();
    private readonly WadevoLabeledTextBox _cooldownBox = new();
    private readonly WadevoLabeledTextBox _durationSecondsBox = new();
    private readonly WadevoCheckBox _enabledBox = new();
    private readonly WadevoDesignerShell _designerShell = new();

    private readonly string _id;
    private string? _linkedOverlayPresetId;

    public AlertStudioControl(AlertProfileModel profile)
    {
        _id = profile.Id;
        _linkedOverlayPresetId = profile.LinkedOverlayPresetId;

        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;
        Padding = new Padding(0);

        Panel configRow = new()
        {
            Location = new Point(0, 0),
            Size = new Size(Math.Max(0, ClientSize.Width), 112),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
        };

        _nameBox.LabelText = "Alert name";
        _nameBox.TextValue = profile.Name;
        _nameBox.Location = new Point(4, 4);
        _nameBox.Size = new Size(260, 58);

        Label triggerLabel = new()
        {
            Text = "Event trigger (pick one, or type your own)",
            Location = new Point(276, 4),
            Size = new Size(280, 18),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _triggerCombo.DropDownStyle = ComboBoxStyle.DropDown;
        _triggerCombo.Location = new Point(276, 24);
        _triggerCombo.Size = new Size(280, 28);
        _triggerCombo.Font = WadevoTheme.Fonts.Default;

        foreach ((string trigger, string description) in KnownTriggers)
        {
            _triggerCombo.Items.Add(string.IsNullOrEmpty(trigger) ? description : trigger + " - " + description);
        }

        (string Trigger, string Description) match = KnownTriggers.FirstOrDefault(t => t.Trigger == profile.EventTrigger);

        _triggerCombo.Text = match.Trigger == profile.EventTrigger && !string.IsNullOrEmpty(match.Trigger)
            ? match.Trigger + " - " + match.Description
            : profile.EventTrigger;

        _cooldownBox.LabelText = "Cooldown (sec, 0 = none)";
        _cooldownBox.TextValue = profile.CooldownSeconds.ToString();
        _cooldownBox.Location = new Point(568, 4);
        _cooldownBox.Size = new Size(170, 58);

        _durationSecondsBox.LabelText = "Shows for (seconds)";
        _durationSecondsBox.TextValue = (profile.DurationMilliseconds / 1000.0).ToString("0.#");
        _durationSecondsBox.Location = new Point(750, 4);
        _durationSecondsBox.Size = new Size(160, 58);

        _enabledBox.Text = "Enabled";
        _enabledBox.Location = new Point(568, 68);
        _enabledBox.Size = new Size(160, 28);
        _enabledBox.Font = WadevoTheme.Fonts.Default;
        _enabledBox.ForeColor = WadevoTheme.Colors.Text;
        _enabledBox.Checked = profile.IsEnabled;

        Label designHint = new()
        {
            Text = "🎨 Design its appearance right below - background image/video/GIF, text, sizing, all on the same canvas as every other overlay.",
            Location = new Point(4, 66),
            Size = new Size(552, 34),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        configRow.Controls.Add(_nameBox);
        configRow.Controls.Add(triggerLabel);
        configRow.Controls.Add(_triggerCombo);
        configRow.Controls.Add(_cooldownBox);
        configRow.Controls.Add(_durationSecondsBox);
        configRow.Controls.Add(_enabledBox);
        configRow.Controls.Add(designHint);

        _designerShell.Dock = DockStyle.None;
        _designerShell.Location = new Point(0, 112);
        _designerShell.Size = new Size(Math.Max(0, ClientSize.Width), Math.Max(0, ClientSize.Height - 112));
        _designerShell.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Controls.Add(_designerShell);
        Controls.Add(configRow);

        if (_linkedOverlayPresetId is not null)
        {
            WadevoDesignerPresetModel? existing = new WadevoDesignerPresetStore()
                .LoadAll()
                .FirstOrDefault(p => p.Id == _linkedOverlayPresetId);

            if (existing is not null)
            {
                _designerShell.LoadPreset(existing);
            }
            else
            {
                // The linked design was deleted from under this alert (e.g. removed
                // directly in a saved-overlay list somewhere) - start fresh rather than
                // silently pointing at nothing.
                _linkedOverlayPresetId = null;
                _designerShell.OverlayType = "Alert";
                _designerShell.ClearToBlank();
            }
        }
        else
        {
            // A brand new alert has no design yet - starts genuinely blank, same as a
            // Custom overlay, rather than the Song ID template's artist/song fields,
            // which have nothing to do with an alert.
            _designerShell.OverlayType = "Alert";
            _designerShell.ClearToBlank();
        }
    }

    private string GetTriggerValue()
    {
        string text = _triggerCombo.Text.Trim();

        int separatorIndex = text.IndexOf(" - ", StringComparison.Ordinal);

        return separatorIndex > 0 ? text[..separatorIndex] : text;
    }

    private static int ParsePositiveOrZeroInt(string text, int fallback)
    {
        return int.TryParse(text, out int value) && value >= 0 ? value : fallback;
    }

    private static double? ParseSeconds(string text, double fallback)
    {
        return double.TryParse(text, out double value) && value > 0 ? value : fallback;
    }

    // Persists the embedded canvas's current state to this alert's linked design,
    // creating one on first save if it doesn't have one yet. Called explicitly by the
    // Alerts tab's own Save/Test actions - kept separate from GetProfile() (a pure read
    // of the form fields) since a caller like Duplicate needs to decide for itself whether
    // a copy should share this same design or get its own independent one.
    public void SaveEmbeddedDesign()
    {
        WadevoDesignerPresetStore store = new();
        string presetName = string.IsNullOrWhiteSpace(_nameBox.TextValue.Trim())
            ? "Untitled Alert"
            : _nameBox.TextValue.Trim();

        if (_linkedOverlayPresetId is null)
        {
            WadevoDesignerPresetModel created = store.SavePreset(
                presetName,
                "Alert",
                _designerShell.GetCurrentElements(),
                _designerShell.GetStyleSettings());

            _linkedOverlayPresetId = created.Id;
            return;
        }

        bool wasUpdated = store.UpdatePreset(
            _linkedOverlayPresetId,
            _designerShell.GetCurrentElements(),
            _designerShell.GetStyleSettings());

        if (!wasUpdated)
        {
            // The preset this alert thought it owned no longer exists in the store (same
            // deleted-out-from-under-it case as in the constructor) - create a fresh one
            // rather than silently failing to save the design at all.
            WadevoDesignerPresetModel created = store.SavePreset(
                presetName,
                "Alert",
                _designerShell.GetCurrentElements(),
                _designerShell.GetStyleSettings());

            _linkedOverlayPresetId = created.Id;
        }
    }

    // Creates and links a brand new, independent copy of this alert's current design,
    // returning its id - used when duplicating an alert, so the copy can be tweaked
    // without also changing the original's appearance.
    public string DuplicateEmbeddedDesign(string newAlertName)
    {
        WadevoDesignerPresetModel duplicate = new WadevoDesignerPresetStore().SavePreset(
            $"{newAlertName} Design",
            "Alert",
            _designerShell.GetCurrentElements(),
            _designerShell.GetStyleSettings());

        return duplicate.Id;
    }

    public AlertProfileModel GetProfile()
    {
        int durationMs = ParseSeconds(_durationSecondsBox.TextValue, 4.2) is double seconds
            ? (int)(seconds * 1000)
            : 4200;

        return new AlertProfileModel
        {
            Id = _id,
            Name = _nameBox.TextValue.Trim(),
            EventTrigger = GetTriggerValue(),
            LinkedOverlayPresetId = _linkedOverlayPresetId,
            CooldownSeconds = ParsePositiveOrZeroInt(_cooldownBox.TextValue, 0),
            DurationMilliseconds = durationMs,
            IsEnabled = _enabledBox.Checked
        };
    }
}
