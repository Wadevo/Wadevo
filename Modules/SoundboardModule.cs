namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Soundboard;

public sealed class SoundboardModule : WadevoModule
{
    private readonly SoundboardSettingsService _settingsService = new();
    private readonly SoundLibraryService _libraryService = new();
    private readonly SoundPlaybackService _playbackService = new();
    private readonly GlobalHotkeyService _hotkeyService = new();

    private readonly Panel _toolbarPanel = new();
    private readonly Label _statusLabel = new();
    private readonly WadevoScrollablePanel _resultsPanel = new();
    private readonly Label _emptyStateLabel = new();

    private readonly WadevoButton _addSoundsButton = new();
    private readonly WadevoButton _stopAllButton = new();
    private readonly TrackBar _masterVolumeTrackBar = new();
    private readonly Label _masterVolumeLabel = new();
    private readonly Label _outputDeviceLabel = new();
    private readonly WadevoComboBox _outputDeviceCombo = new();
    private const string SystemDefaultDeviceOption = "System Default";

    private SoundboardSettingsModel _settings;

    // The clip currently waiting for a key press to bind as its hotkey, plus the button
    // whose text should reset if listening is cancelled or completed.
    private Guid? _listeningClipId;
    private WadevoButton? _listeningButton;

    public override string ModuleName => "Soundboard";

    public override string ModuleDescription =>
        "Import local sounds and bind them to hotkeys you can hit mid-game, no Stream Deck required.";

    public SoundboardModule()
    {
        BackColor = WadevoTheme.Colors.Background;
        Padding = new Padding(0);
        AutoScroll = false;
        Dock = DockStyle.Fill;

        _settings = _settingsService.Load();

        BuildToolbar();
        BuildStatusLabel();
        BuildResultsPanel();

        Controls.Add(_resultsPanel);
        Controls.Add(_statusLabel);
        Controls.Add(_toolbarPanel);

        _hotkeyService.HotkeyPressed += clipId => PlayClip(clipId);

        Disposed += (_, _) =>
        {
            _hotkeyService.Dispose();
            _playbackService.StopAll();
        };

        RegisterAllHotkeys();
        RefreshGrid();
    }

    private void PopulateOutputDeviceCombo()
    {
        _outputDeviceCombo.Items.Clear();
        _outputDeviceCombo.Items.Add(SystemDefaultDeviceOption);

        foreach (string deviceName in SoundPlaybackService.GetAvailableDeviceNames())
        {
            _outputDeviceCombo.Items.Add(deviceName);
        }

        int selectedIndex = string.IsNullOrWhiteSpace(_settings.OutputDeviceName)
            ? 0
            : _outputDeviceCombo.Items.IndexOf(_settings.OutputDeviceName);

        // The previously-saved device might not be plugged in right now - falls back to
        // showing System Default rather than an index-out-of-range error.
        _outputDeviceCombo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
    }

    private void BuildToolbar()
    {
        _toolbarPanel.Dock = DockStyle.Top;
        _toolbarPanel.Height = 148;
        _toolbarPanel.BackColor = Color.Transparent;
        _toolbarPanel.Padding = new Padding(0, 0, 0, 12);

        WadevoGlassCard toolbarCard = new()
        {
            Dock = DockStyle.Fill,
            AccentColor = WadevoTheme.Colors.Accent,
            Padding = new Padding(20)
        };

        Label toolbarHeader = new()
        {
            Text = "🔊 Playback Controls",
            Location = new Point(20, 14),
            Size = new Size(300, 26),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent
        };

        _addSoundsButton.ButtonText = "Add Sounds";
        _addSoundsButton.Location = new Point(20, 56);
        _addSoundsButton.Size = new Size(140, 40);
        _addSoundsButton.ButtonClicked += (_, _) => ImportSounds();

        _stopAllButton.ButtonText = "Stop All";
        _stopAllButton.Location = new Point(172, 56);
        _stopAllButton.Size = new Size(110, 40);
        _stopAllButton.AccentColor = WadevoTheme.Colors.Error;
        _stopAllButton.ButtonClicked += (_, _) => _playbackService.StopAll();

        _masterVolumeLabel.Text = "Master Volume";
        _masterVolumeLabel.Location = new Point(312, 36);
        _masterVolumeLabel.Size = new Size(120, 20);
        _masterVolumeLabel.Font = WadevoTheme.Fonts.Small;
        _masterVolumeLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _masterVolumeLabel.BackColor = Color.Transparent;

        _masterVolumeTrackBar.Minimum = 0;
        _masterVolumeTrackBar.Maximum = 100;
        _masterVolumeTrackBar.Value = Math.Clamp(_settings.MasterVolume, 0, 100);
        _masterVolumeTrackBar.Location = new Point(312, 58);
        _masterVolumeTrackBar.Size = new Size(170, 36);
        _masterVolumeTrackBar.TickStyle = TickStyle.None;
        _masterVolumeTrackBar.Scroll += (_, _) =>
        {
            _settings.MasterVolume = _masterVolumeTrackBar.Value;
            _settingsService.Save(_settings);
        };

        _outputDeviceLabel.Text = "Output Device";
        _outputDeviceLabel.Location = new Point(508, 36);
        _outputDeviceLabel.Size = new Size(160, 20);
        _outputDeviceLabel.Font = WadevoTheme.Fonts.Small;
        _outputDeviceLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _outputDeviceLabel.BackColor = Color.Transparent;

        // Height matched exactly to the card's remaining space below it (card padding 20 +
        // header 40 + this row's own top offset), so it can never be clipped by a
        // container that's shorter than what this control actually needs - the previous
        // layout had the combo positioned past the bottom of its own parent panel.
        _outputDeviceCombo.Location = new Point(508, 58);
        _outputDeviceCombo.Size = new Size(260, 32);
        _outputDeviceCombo.Font = WadevoTheme.Fonts.Default;
        _outputDeviceCombo.ForeColor = WadevoTheme.Colors.Text;
        _outputDeviceCombo.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _outputDeviceCombo.FlatStyle = FlatStyle.Flat;
        _outputDeviceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        PopulateOutputDeviceCombo();

        _outputDeviceCombo.SelectedIndexChanged += (_, _) =>
        {
            string selected = _outputDeviceCombo.Text.Trim();
            _settings.OutputDeviceName = selected == SystemDefaultDeviceOption ? "" : selected;
            _settingsService.Save(_settings);
        };

        toolbarCard.Controls.Add(toolbarHeader);
        toolbarCard.Controls.Add(_outputDeviceLabel);
        toolbarCard.Controls.Add(_outputDeviceCombo);
        toolbarCard.Controls.Add(_addSoundsButton);
        toolbarCard.Controls.Add(_stopAllButton);
        toolbarCard.Controls.Add(_masterVolumeLabel);
        toolbarCard.Controls.Add(_masterVolumeTrackBar);

        _toolbarPanel.Controls.Add(toolbarCard);
    }

    private void BuildStatusLabel()
    {
        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Height = 28;
        _statusLabel.Text = "● Soundboard ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
        _statusLabel.Font = WadevoTheme.Fonts.Small;
        _statusLabel.BackColor = Color.Transparent;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
    }

    private void BuildResultsPanel()
    {
        _resultsPanel.Dock = DockStyle.Fill;
        _resultsPanel.BackColor = Color.Transparent;
        _resultsPanel.Content.WrapContents = true;
        _resultsPanel.Content.FlowDirection = FlowDirection.LeftToRight;
        _resultsPanel.Content.Padding = new Padding(0, 8, 0, 8);

        _emptyStateLabel.Text =
            "No sounds yet. Click \"Add Sounds\" to import MP3 or WAV clips and bind hotkeys to them.";
        _emptyStateLabel.AutoSize = false;
        _emptyStateLabel.Size = new Size(520, 60);
        _emptyStateLabel.Font = WadevoTheme.Fonts.Medium;
        _emptyStateLabel.ForeColor = WadevoTheme.Colors.TextSecondary;
        _emptyStateLabel.BackColor = Color.Transparent;
        _emptyStateLabel.Margin = new Padding(8);
    }

    private void ImportSounds()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Add sounds",
            Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav",
            Multiselect = true
        };

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        foreach (string sourcePath in dialog.FileNames)
        {
            try
            {
                string libraryPath = _libraryService.Import(sourcePath);

                SoundClipModel clip = new()
                {
                    Name = Path.GetFileNameWithoutExtension(sourcePath),
                    FilePath = libraryPath,
                    Volume = 100
                };

                _settings.Clips.Insert(0, clip);
            }
            catch (Exception ex)
            {
                WadevoLogger.Error("Failed to import sound", ex);
                SetStatus($"Couldn't import \"{Path.GetFileName(sourcePath)}\": {ex.Message}", WadevoTheme.Colors.Error);
            }
        }

        _settingsService.Save(_settings);
        SetStatus($"● Soundboard ready — {_settings.Clips.Count} sound(s) loaded", WadevoTheme.Colors.Success);
        RefreshGrid();
    }

    private void RegisterAllHotkeys()
    {
        foreach (SoundClipModel clip in _settings.Clips)
        {
            if (!clip.HasHotkey)
            {
                continue;
            }

            GlobalHotkeyService.HotkeyModifiers modifiers = HotkeyFormatting.ParseModifiers(clip.HotkeyModifiers);

            if (Enum.TryParse(clip.HotkeyKey, out Keys key))
            {
                _hotkeyService.Register(clip.Id, modifiers, key);
            }
        }
    }

    private void RefreshGrid()
    {
        Control[] oldControls = _resultsPanel.Content.Controls.Cast<Control>().ToArray();
        _resultsPanel.Content.Controls.Clear();

        foreach (Control control in oldControls)
        {
            control.Dispose();
        }

        if (_settings.Clips.Count == 0)
        {
            _resultsPanel.Content.Controls.Add(_emptyStateLabel);
            _resultsPanel.RefreshLayout();
            return;
        }

        foreach (SoundClipModel clip in _settings.Clips)
        {
            _resultsPanel.Content.Controls.Add(CreateClipTile(clip));
        }

        _resultsPanel.RefreshLayout();
    }

    private Control CreateClipTile(SoundClipModel clip)
    {
        WadevoCard card = new()
        {
            Width = 270,
            Height = 252,
            Margin = new Padding(8),
            Padding = new Padding(0),
            ShowAccent = true,
            BackColor = WadevoTheme.Colors.Card,
            CornerRadius = 10,
            Tag = clip.Id
        };

        // Plain text instead of a round button - sidesteps the corner-curve clipping issue
        // entirely rather than needing to fit a filled shape precisely next to it. Three
        // attempts at getting a round button's exact safe position right (each one making
        // a different small error in the curve math) is a sign the right fix is removing
        // the geometry risk, not getting better at calculating around it.
        Label nameLabel = new()
        {
            Text = clip.Name,
            Location = new Point(16, 18),
            Size = new Size(160, 22),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        Label removeLabel = new()
        {
            Text = "✕ Remove",
            Location = new Point(182, 20),
            Size = new Size(76, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Error,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleRight
        };
        removeLabel.Click += (_, _) => RemoveClip(clip);

        WadevoButton playButton = new()
        {
            ButtonText = "▶  Play",
            Location = new Point(16, 66),
            Size = new Size(234, 46),
            AccentColor = WadevoTheme.Colors.Accent
        };
        playButton.ButtonClicked += (_, _) => PlayClip(clip.Id);

        Label hotkeyLabel = new()
        {
            Text = HotkeyFormatting.DisplayText(clip.HotkeyModifiers, clip.HotkeyKey),
            Location = new Point(16, 122),
            Size = new Size(234, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = clip.HasHotkey ? WadevoTheme.Colors.Cyan : WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        WadevoButton hotkeyButton = new()
        {
            ButtonText = "Set Hotkey",
            Location = new Point(16, 148),
            Size = new Size(234, 36)
        };
        hotkeyButton.ButtonClicked += (_, _) => BeginHotkeyCapture(clip, hotkeyButton, hotkeyLabel);

        Label volumeLabel = new()
        {
            Text = $"Volume {clip.Volume}%",
            Location = new Point(16, 196),
            Size = new Size(234, 16),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        TrackBar volumeTrackBar = new()
        {
            Minimum = 0,
            Maximum = 100,
            Value = Math.Clamp(clip.Volume, 0, 100),
            Location = new Point(10, 214),
            Size = new Size(250, 28),
            TickStyle = TickStyle.None
        };
        volumeTrackBar.Scroll += (_, _) =>
        {
            clip.Volume = volumeTrackBar.Value;
            volumeLabel.Text = $"Volume {clip.Volume}%";
            _settingsService.Save(_settings);
        };

        card.Controls.Add(nameLabel);
        card.Controls.Add(removeLabel);
        card.Controls.Add(playButton);
        card.Controls.Add(hotkeyLabel);
        card.Controls.Add(hotkeyButton);
        card.Controls.Add(volumeLabel);
        card.Controls.Add(volumeTrackBar);

        return card;
    }

    private void BeginHotkeyCapture(SoundClipModel clip, WadevoButton button, Label hotkeyLabel)
    {
        // Starting a new capture cancels any capture already in progress on another tile -
        // only one tile can be "listening" for a key press at a time.
        if (_listeningButton is { IsDisposed: false })
        {
            _listeningButton.ButtonText = "Set Hotkey";
        }

        _listeningClipId = clip.Id;
        _listeningButton = button;
        button.ButtonText = "Press keys… (Esc cancels)";
        Focus();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_listeningClipId is Guid clipId)
        {
            Keys baseKey = keyData & Keys.KeyCode;

            if (baseKey == Keys.Escape)
            {
                CancelHotkeyCapture();
                return true;
            }

            // A bare modifier press (nothing else held yet) isn't a usable hotkey on its
            // own - keep listening until a real key comes down alongside it.
            bool isModifierOnly =
                baseKey is Keys.ControlKey or Keys.ShiftKey or Keys.Menu
                    or Keys.LWin or Keys.RWin or Keys.None;

            if (isModifierOnly)
            {
                return true;
            }

            Keys modifierFlags = keyData & Keys.Modifiers;
            GlobalHotkeyService.HotkeyModifiers modifiers = GlobalHotkeyService.HotkeyModifiers.None;

            if (modifierFlags.HasFlag(Keys.Control)) modifiers |= GlobalHotkeyService.HotkeyModifiers.Control;
            if (modifierFlags.HasFlag(Keys.Alt)) modifiers |= GlobalHotkeyService.HotkeyModifiers.Alt;
            if (modifierFlags.HasFlag(Keys.Shift)) modifiers |= GlobalHotkeyService.HotkeyModifiers.Shift;

            CompleteHotkeyCapture(clipId, modifiers, baseKey);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void CompleteHotkeyCapture(Guid clipId, GlobalHotkeyService.HotkeyModifiers modifiers, Keys key)
    {
        SoundClipModel? clip = _settings.Clips.FirstOrDefault(c => c.Id == clipId);

        _listeningClipId = null;
        _listeningButton = null;

        if (clip is null)
        {
            return;
        }

        bool registered = _hotkeyService.Register(clipId, modifiers, key);

        if (!registered)
        {
            SetStatus(
                $"Couldn't bind that combo — it may already be in use by another app. Try a different key.",
                WadevoTheme.Colors.Error);
            RefreshGrid();
            return;
        }

        clip.HotkeyModifiers = HotkeyFormatting.FormatModifiers(modifiers);
        clip.HotkeyKey = key.ToString();
        _settingsService.Save(_settings);

        SetStatus(
            $"● Bound \"{clip.Name}\" to {HotkeyFormatting.DisplayText(clip.HotkeyModifiers, clip.HotkeyKey)}",
            WadevoTheme.Colors.Success);

        RefreshGrid();
    }

    private void CancelHotkeyCapture()
    {
        if (_listeningButton is { IsDisposed: false })
        {
            _listeningButton.ButtonText = "Set Hotkey";
        }

        _listeningClipId = null;
        _listeningButton = null;
    }

    private void PlayClip(Guid clipId)
    {
        SoundClipModel? clip = _settings.Clips.FirstOrDefault(c => c.Id == clipId);

        if (clip is null)
        {
            return;
        }

        try
        {
            int effectiveVolume = clip.Volume * _settings.MasterVolume / 100;
            _playbackService.Play(clip.FilePath, effectiveVolume, _settings.OutputDeviceName);
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Failed to play sound", ex);
            SetStatus($"Couldn't play \"{clip.Name}\": {ex.Message}", WadevoTheme.Colors.Error);
        }
    }

    private void RemoveClip(SoundClipModel clip)
    {
        _hotkeyService.Unregister(clip.Id);
        _libraryService.Remove(clip.FilePath);
        _settings.Clips.Remove(clip);
        _settingsService.Save(_settings);

        SetStatus($"● Removed \"{clip.Name}\"", WadevoTheme.Colors.TextMuted);
        RefreshGrid();
    }

    private void SetStatus(string text, Color color)
    {
        if (IsDisposed)
        {
            return;
        }

        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }
}
