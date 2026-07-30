namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Services;

public sealed class NowPlayingPanelControl : UserControl
{
    private readonly WadevoDesignerPresetStore _presetStore = new();
    private readonly LiveOverlaySettingsStore _settingsStore = new();
    private readonly WadevoComboBox _overlayCombo = new();
    private readonly Label _identityLabel = new();
    private List<WadevoDesignerPresetModel> _choices = new();

    public NowPlayingPanelControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        // The WorkspacePanelCard hosting this already shows "🔴 Live Overlay" as its own
        // chrome title - a second identical label here was pure duplication and gave no
        // indication of which overlay was actually selected. This shows the overlay's
        // actual name instead, which is the thing that's genuinely useful to see at a
        // glance from across a stream deck of panels.
        _identityLabel.Location = new Point(8, 8);
        _identityLabel.Size = new Size(260, 24);
        _identityLabel.Font = WadevoTheme.Fonts.Bold;
        _identityLabel.ForeColor = WadevoTheme.Colors.Accent;
        _identityLabel.BackColor = Color.Transparent;
        _identityLabel.AutoEllipsis = true;
        _identityLabel.Text = "No overlay selected";

        _overlayCombo.Location = new Point(8, 36);
        _overlayCombo.Size = new Size(240, 28);
        _overlayCombo.DropDownStyle = ComboBoxStyle.DropDownList;

        Label hint = new()
        {
            Text = "Pick which saved overlay is broadcasting right now.",
            Location = new Point(8, 72),
            Size = new Size(260, 40),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Controls.Add(_identityLabel);
        Controls.Add(_overlayCombo);
        Controls.Add(hint);

        HandleCreated += (_, _) => RefreshChoices();

        _overlayCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_overlayCombo.SelectedItem is WadevoDesignerPresetModel selected)
            {
                _settingsStore.SetLivePresetId(selected.Id);
                _identityLabel.Text = $"🔴 {selected.Name}";
            }
        };
    }

    private void RefreshChoices()
    {
        _choices = _presetStore.LoadAll();
        string? livePresetId = _settingsStore.GetLivePresetId();

        _overlayCombo.DataSource = null;
        _overlayCombo.DataSource = _choices;
        _overlayCombo.DisplayMember = nameof(WadevoDesignerPresetModel.Name);

        if (livePresetId is not null)
        {
            int index = _choices.FindIndex(preset => preset.Id == livePresetId);

            if (index >= 0)
            {
                _overlayCombo.SelectedIndex = index;
                _identityLabel.Text = $"🔴 {_choices[index].Name}";
                return;
            }
        }

        _identityLabel.Text = _choices.Count == 0
            ? "No saved overlays yet"
            : "No overlay selected";
    }
}
