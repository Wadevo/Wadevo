namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Modules.OverlayEngine;
using Wadevo.Services;

public sealed class WadevoDesignerShell : UserControl
{
    private readonly OverlayDesignerService _designerService = new();

    private readonly WadevoDesignerCanvas _canvas = new();
    private WadevoButton? _textButton;
    private WadevoButton _zoomButton = new();
    private string _overlayType = "Song ID";

    // The "Text" toolbar button edits sample song data (artist/song/album/iTunes lookup) -
    // that only means anything for a Song ID overlay. Custom and Alert overlays start
    // genuinely blank with no song-data concept at all, so showing this button there was
    // just confusing - text on those overlay types is edited per-widget instead, via
    // "+Add Widget" and double-clicking each one, which already works correctly.
    public string OverlayType
    {
        get => _overlayType;
        set
        {
            _overlayType = value;

            if (_textButton is not null)
            {
                _textButton.Visible = _overlayType == "Song ID";
            }
        }
    }

    public WadevoDesignerShell()
    {
        Dock = DockStyle.Fill;
        BackColor = WadevoTheme.Colors.Background;
        Padding = new Padding(18);

        OverlayThemeModel selectedTheme = _designerService.GetSelectedTheme();

        _canvas.Theme = selectedTheme;
        // Deliberately not seeding PreviewTitle/Artist/Song from the shared global settings
        // here - a fresh overlay starts from the canvas's own generic placeholder defaults.
        // LoadPreset() below still correctly restores an existing overlay's own saved text.
        // This is also a static sample preview, not a live feed - no need to design an
        // overlay while actually streaming, so there's nothing here that could add load
        // during a real stream.

        Panel topBar = BuildTopBar();

        WadevoGlassCard canvasCard = new()
        {
            Dock = DockStyle.Fill,
            AccentColor = WadevoTheme.Colors.Cyan,
            Padding = new Padding(0)
        };

        Panel canvasViewport = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        // The canvas sets Dock=Fill on itself in its own constructor for its previous
        // single-consumer assumption (always exactly filling its parent) - zoom needs to
        // physically resize the control itself, which Dock=Fill would just override back
        // to "fill the parent" on every layout pass, so it's switched off here in favor of
        // an explicit size that SetZoom controls directly, inside a scrollable viewport so
        // zooming in (making the canvas bigger than the visible card) can still be panned.
        _canvas.Dock = DockStyle.None;
        _canvas.Location = new Point(0, 0);

        canvasViewport.Controls.Add(_canvas);
        canvasCard.Controls.Add(canvasViewport);

        Controls.Add(canvasCard);
        Controls.Add(topBar);

        _canvas.SetHostViewport(canvasViewport);
        _canvas.SetZoom(1.0f);

        // The viewport's ClientSize at this exact point in construction may still be
        // WinForms' small default rather than its true final size, if this control hasn't
        // actually been mounted into a visible parent chain yet - the same class of bug
        // hit earlier with Alert Studio's canvas. Load fires once this control is truly
        // part of the visible tree with its real size, giving a guaranteed second, correct
        // fit regardless of whether a genuine resize happens to occur afterward too.
        Load += (_, _) => _canvas.SetZoom(_canvas.ZoomFactor);
    }

    // Called by the outer Overlay Designer page when opening a specific saved overlay.
    public void LoadPreset(WadevoDesignerPresetModel preset)
    {
        OverlayType = preset.OverlayType;

        _canvas.LoadElements(preset.Elements);
        _canvas.SetBackgroundImage(string.IsNullOrWhiteSpace(preset.BackgroundImagePath) ? null : preset.BackgroundImagePath);

        WadevoBackgroundScaleMode scaleMode = Enum.TryParse(preset.BackgroundScaleMode, out WadevoBackgroundScaleMode parsed)
            ? parsed
            : WadevoBackgroundScaleMode.Fill;

        _canvas.SetBackgroundStyle(scaleMode, preset.BackgroundRoundedCorners, preset.BackgroundWidthPercent, preset.BackgroundHeightPercent, preset.BackgroundOpacityPercent);
        _canvas.SetBackgroundOffset(preset.BackgroundOffsetX, preset.BackgroundOffsetY);

        _canvas.AnimationType = preset.AnimationType;
        _canvas.AnimationDurationMs = preset.AnimationDurationMs;
        _canvas.AutoHideSeconds = preset.AutoHideSeconds;
        _canvas.AlwaysOn = preset.AlwaysOn;
    }

    // Called by the outer Overlay Designer page when starting a blank "Custom" overlay,
    // instead of the default Song ID starting layout.
    public void ClearToBlank()
    {
        _canvas.ClearAllElements();
    }

    // Called by the outer Overlay Designer page when saving the current state.
    public IReadOnlyList<WadevoDesignerElementState> GetCurrentElements()
    {
        return _canvas.GetElementsSnapshot();
    }

    // Called by the outer Overlay Designer page when saving the current state.
    public WadevoOverlayStyleSettings GetStyleSettings()
    {
        return new WadevoOverlayStyleSettings
        {
            BackgroundImagePath = _canvas.BackgroundImagePath ?? "",
            BackgroundScaleMode = _canvas.BackgroundScaleMode.ToString(),
            BackgroundRoundedCorners = _canvas.BackgroundRoundedCorners,
            BackgroundWidthPercent = _canvas.BackgroundWidthPercent,
            BackgroundHeightPercent = _canvas.BackgroundHeightPercent,
            BackgroundOpacityPercent = _canvas.BackgroundOpacityPercent,
            BackgroundOffsetX = _canvas.BackgroundOffsetX,
            BackgroundOffsetY = _canvas.BackgroundOffsetY,
            AnimationType = _canvas.AnimationType,
            AnimationDurationMs = _canvas.AnimationDurationMs,
            AutoHideSeconds = _canvas.AutoHideSeconds,
            AlwaysOn = _canvas.AlwaysOn
        };
    }

    private Panel BuildTopBar()
    {
        Panel bar = new()
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 12)
        };

        WadevoButton textButton = CreateDropdownButton("Text ▾", WadevoTheme.Colors.Cyan);
        _textButton = textButton;
        textButton.Visible = _overlayType == "Song ID";
        textButton.Location = new Point(0, 0);
        textButton.ButtonClicked += (_, _) => ShowTextDropdown(textButton);

        WadevoButton motionButton = CreateDropdownButton("Motion ▾", WadevoTheme.Colors.Purple);
        motionButton.Location = new Point(textButton.Right + 10, 0);
        motionButton.ButtonClicked += (_, _) => ShowMotionDropdown(motionButton);

        WadevoButton backgroundButton = CreateDropdownButton("Background ▾", WadevoTheme.Colors.Accent);
        backgroundButton.Location = new Point(motionButton.Right + 10, 0);
        backgroundButton.ButtonClicked += (_, _) => ShowBackgroundDropdown(backgroundButton);

        WadevoButton addWidgetButton = CreateDropdownButton("+ Add Widget ▾", WadevoTheme.Colors.Success);
        addWidgetButton.Location = new Point(backgroundButton.Right + 10, 0);
        addWidgetButton.ButtonClicked += (_, _) => ShowAddWidgetDropdown(addWidgetButton);

        _zoomButton = CreateDropdownButton("🔍 100% ▾", WadevoTheme.Colors.Cyan);
        _zoomButton.Size = new Size(110, 40);
        _zoomButton.Location = new Point(addWidgetButton.Right + 10, 0);
        _zoomButton.ButtonClicked += (_, _) => ShowZoomDropdown(_zoomButton);

        Label editHintLabel = new()
        {
            Text = "💡 Double-click any widget on the canvas to edit its text, font, or settings.",
            Location = new Point(0, 44),
            Size = new Size(560, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        bar.Controls.Add(textButton);
        bar.Controls.Add(motionButton);
        bar.Controls.Add(backgroundButton);
        bar.Controls.Add(addWidgetButton);
        bar.Controls.Add(_zoomButton);
        bar.Controls.Add(editHintLabel);

        return bar;
    }

    private static WadevoButton CreateDropdownButton(string text, Color color)
    {
        return new WadevoButton
        {
            ButtonText = text,
            Size = new Size(168, 40),
            AccentColor = color
        };
    }

    private void ShowTextDropdown(Control anchor)
    {
        OverlaySettingsModel settings = _designerService.GetPreviewSettings();

        WadevoPropertyPage page = new()
        {
            Dock = DockStyle.Fill
        };

        WadevoLabeledTextBox titleBox = new()
        {
            LabelText = "Overlay label",
            TextValue = settings.NowPlayingTitle
        };

        WadevoLabeledTextBox artistBox = new()
        {
            LabelText = "Sample artist",
            TextValue = settings.SampleArtist
        };

        WadevoLabeledTextBox songBox = new()
        {
            LabelText = "Sample song",
            TextValue = settings.SampleSong
        };

        WadevoLabeledTextBox albumBox = new()
        {
            LabelText = "Album",
            TextValue = _canvas.PreviewAlbum
        };

        WadevoLabeledTextBox releaseDateBox = new()
        {
            LabelText = "Release date",
            TextValue = _canvas.PreviewReleaseDate
        };

        void SaveTextSettings()
        {
            _designerService.UpdatePreview(
                titleBox.TextValue,
                artistBox.TextValue,
                songBox.TextValue);

            _canvas.SetPreviewText(titleBox.TextValue, artistBox.TextValue, songBox.TextValue);
        }

        titleBox.TextValueChanged += (_, _) => SaveTextSettings();
        artistBox.TextValueChanged += (_, _) => SaveTextSettings();
        songBox.TextValueChanged += (_, _) => SaveTextSettings();

        albumBox.TextValueChanged += (_, _) => { _canvas.PreviewAlbum = albumBox.TextValue; };
        releaseDateBox.TextValueChanged += (_, _) => { _canvas.PreviewReleaseDate = releaseDateBox.TextValue; };

        WadevoButton lookupButton = new()
        {
            ButtonText = "🔍 Look Up From iTunes",
            Size = new Size(240, 38)
        };

        Label lookupStatus = new()
        {
            Text = "",
            Size = new Size(300, 22),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        lookupButton.ButtonClicked += async (_, _) => await LookupArtworkAsync(
            artistBox, songBox, albumBox, releaseDateBox, lookupButton, lookupStatus);

        page.AddRange(
            titleBox, artistBox, songBox, albumBox, releaseDateBox,
            lookupButton, lookupStatus);

        WadevoDropdownPopup popup = new(page, 380, 460);
        popup.ShowBelow(anchor);
    }

    private void ShowAddWidgetDropdown(Control anchor)
    {
        Panel content = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WadevoTheme.Colors.Background,
            Padding = new Padding(16)
        };

        Label header = new()
        {
            Text = "Add a Widget",
            Location = new Point(16, 12),
            Size = new Size(220, 24),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        content.Controls.Add(header);

        (string Label, string Icon, Action Action)[] widgetOptions =
        {
            ("Text", "🔤", () =>
            {
                _canvas.AddTextWidget();
                WadevoMessageBox.Show(
                    FindForm(),
                    "Added a new text widget to the canvas.\n\nDouble-click it to change its text. Click to select it, then press Delete to remove it.",
                    "Text Widget Added");
            }),
            ("Image / Logo", "🖼", () =>
            {
                using OpenFileDialog dialog = new()
                {
                    Filter = "Image or video files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.mp4;*.webm;*.mov)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.mp4;*.webm;*.mov",
                    Title = "Choose an image or video"
                };

                if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                {
                    return;
                }

                _canvas.AddImageWidget(dialog.FileName);
            }),
            ("Clock", "🕐", () => _canvas.AddClockWidget()),
            ("Countdown", "⏱", () => _canvas.AddCountdownWidget()),
            ("Song Queue", "🎵", () => _canvas.AddSongQueueWidget()),
            ("Goal Bar", "📈", () => _canvas.AddGoalBarWidget()),
            ("Vote Tally", "🗳️", () => _canvas.AddVoteTallyWidget()),
            ("Chat Feed", "💬", () => _canvas.AddChatFeedWidget())
        };

        int y = 48;

        foreach ((string label, string icon, Action action) in widgetOptions)
        {
            WadevoButton optionButton = new()
            {
                ButtonText = $"{icon} {label}",
                Location = new Point(16, y),
                Size = new Size(220, 38),
                AccentColor = WadevoTheme.Colors.Success
            };

            optionButton.ButtonClicked += (_, _) => action();

            content.Controls.Add(optionButton);

            y += 46;
        }

        WadevoDropdownPopup popup = new(content, 256, y + 16);
        popup.ShowBelow(anchor);
    }

    private void ShowZoomDropdown(Control anchor)
    {
        Panel content = new()
        {
            BackColor = WadevoTheme.Colors.Background
        };

        (string Label, float Zoom)[] presets =
        {
            ("50%", 0.5f),
            ("75%", 0.75f),
            ("100% (Actual Size)", 1.0f),
            ("125%", 1.25f),
            ("150%", 1.5f),
            ("200%", 2.0f)
        };

        int y = 8;

        foreach ((string label, float zoom) in presets)
        {
            WadevoButton optionButton = new()
            {
                ButtonText = label,
                Location = new Point(8, y),
                Size = new Size(220, 34),
                AccentColor = Math.Abs(_canvas.ZoomFactor - zoom) < 0.01f
                    ? WadevoTheme.Colors.Cyan
                    : WadevoTheme.Colors.TextMuted
            };

            optionButton.ButtonClicked += (_, _) =>
            {
                _canvas.SetZoom(zoom);
                RefreshZoomButtonLabel();
            };

            content.Controls.Add(optionButton);
            y += 40;
        }

        WadevoDropdownPopup popup = new(content, 236, y + 16);
        popup.ShowBelow(anchor);
    }

    private void RefreshZoomButtonLabel()
    {
        _zoomButton.ButtonText = $"🔍 {(int)(_canvas.ZoomFactor * 100)}% ▾";
    }

    private void ShowMotionDropdown(Control anchor)
    {
        Panel content = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WadevoTheme.Colors.Background,
            Padding = new Padding(16)
        };

        Label header = new()
        {
            Text = "Overlay Motion",
            Location = new Point(16, 12),
            Size = new Size(300, 24),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        WadevoCheckBox alwaysOnBox = new()
        {
            Text = "Always on (no entrance/exit animation)",
            Location = new Point(16, 48),
            Size = new Size(320, 26),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = _canvas.AlwaysOn
        };

        Label styleLabel = new()
        {
            Text = "Entrance style",
            Location = new Point(16, 86),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoComboBox styleCombo = new()
        {
            Location = new Point(16, 108),
            Size = new Size(200, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = WadevoTheme.Fonts.Default
        };

        styleCombo.Items.AddRange(new object[] { "Slide Right", "Slide Left", "Fade" });
        styleCombo.SelectedItem = _canvas.AnimationType switch
        {
            "SlideLeft" => "Slide Left",
            "Fade" => "Fade",
            _ => "Slide Right"
        };

        Label durationLabel = new()
        {
            Text = "Duration (seconds)",
            Location = new Point(16, 148),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        NumericUpDown durationBox = new()
        {
            Location = new Point(16, 170),
            Size = new Size(100, 26),
            Minimum = 0.1m,
            Maximum = 3.0m,
            Increment = 0.1m,
            DecimalPlaces = 1,
            // The underlying value is still stored in milliseconds (AnimationDurationMs) -
            // this just presents and edits it in seconds, since nobody thinks in
            // milliseconds when picking how long an entrance animation should feel.
            Value = Math.Clamp(_canvas.AnimationDurationMs / 1000m, 0.1m, 3.0m)
        };

        Label autoHideLabel = new()
        {
            Text = "Auto-hide after (seconds, 0 = stays up)",
            Location = new Point(16, 210),
            Size = new Size(280, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        NumericUpDown autoHideBox = new()
        {
            Location = new Point(16, 232),
            Size = new Size(100, 26),
            Minimum = 0,
            Maximum = 600,
            Increment = 1,
            DecimalPlaces = 0,
            Value = Math.Clamp(_canvas.AutoHideSeconds, 0, 600)
        };

        styleCombo.Enabled = !alwaysOnBox.Checked;
        durationBox.Enabled = !alwaysOnBox.Checked;
        autoHideBox.Enabled = !alwaysOnBox.Checked;

        void ApplyMotionSettings()
        {
            _canvas.AlwaysOn = alwaysOnBox.Checked;

            _canvas.AnimationType = styleCombo.SelectedItem?.ToString() switch
            {
                "Slide Left" => "SlideLeft",
                "Fade" => "Fade",
                _ => "SlideRight"
            };

            _canvas.AnimationDurationMs = (int)(durationBox.Value * 1000m);
            _canvas.AutoHideSeconds = (int)autoHideBox.Value;

            styleCombo.Enabled = !alwaysOnBox.Checked;
            durationBox.Enabled = !alwaysOnBox.Checked;
            autoHideBox.Enabled = !alwaysOnBox.Checked;
        }

        alwaysOnBox.CheckedChanged += (_, _) => ApplyMotionSettings();
        styleCombo.SelectedIndexChanged += (_, _) => ApplyMotionSettings();
        durationBox.ValueChanged += (_, _) => ApplyMotionSettings();
        autoHideBox.ValueChanged += (_, _) => ApplyMotionSettings();

        content.Controls.Add(header);
        content.Controls.Add(alwaysOnBox);
        content.Controls.Add(styleLabel);
        content.Controls.Add(styleCombo);
        content.Controls.Add(durationLabel);
        content.Controls.Add(durationBox);
        content.Controls.Add(autoHideLabel);
        content.Controls.Add(autoHideBox);

        WadevoDropdownPopup popup = new(content, 360, 300);
        popup.ShowBelow(anchor);
    }

    private void ShowBackgroundDropdown(Control anchor)
    {
        Panel content = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WadevoTheme.Colors.Background,
            Padding = new Padding(16)
        };

        Label header = new()
        {
            Text = "Overlay Background",
            Location = new Point(16, 12),
            Size = new Size(300, 24),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label description = new()
        {
            Text = "Optional. Shows behind everything else on this overlay.\nDrag anywhere on empty canvas to reposition it.",
            Location = new Point(16, 42),
            Size = new Size(320, 40),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoButton uploadButton = new()
        {
            ButtonText = "🖼 Upload Background Image",
            Location = new Point(16, 92),
            Size = new Size(260, 40),
            AccentColor = WadevoTheme.Colors.Accent
        };

        WadevoButton clearButton = new()
        {
            ButtonText = "Remove Background",
            Location = new Point(16, 142),
            Size = new Size(260, 38),
            AccentColor = WadevoTheme.Colors.TextMuted
        };

        Label currentStatus = new()
        {
            Text = string.IsNullOrWhiteSpace(_canvas.BackgroundImagePath)
                ? "No background set."
                : $"Current: {Path.GetFileName(_canvas.BackgroundImagePath)}",
            Location = new Point(16, 188),
            Size = new Size(320, 22),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label sizeLabel = new()
        {
            Text = "Size",
            Location = new Point(16, 224),
            Size = new Size(140, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoComboBox sizeCombo = new()
        {
            Location = new Point(16, 246),
            Size = new Size(180, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = WadevoTheme.Fonts.Default
        };

        sizeCombo.Items.AddRange(new object[]
        {
            "Fill (may crop)", "Fit (may letterbox)", "Stretch (may distort)", "Center (actual size)"
        });

        sizeCombo.SelectedIndex = _canvas.BackgroundScaleMode switch
        {
            WadevoBackgroundScaleMode.Fill => 0,
            WadevoBackgroundScaleMode.Fit => 1,
            WadevoBackgroundScaleMode.Stretch => 2,
            WadevoBackgroundScaleMode.Center => 3,
            _ => 0
        };

        WadevoCheckBox roundedBox = new()
        {
            Text = "Rounded corners",
            Location = new Point(16, 286),
            Size = new Size(180, 26),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = _canvas.BackgroundRoundedCorners
        };

        Label resizeHintLabel = new()
        {
            Text = "Drag any corner of the canvas to resize the background directly - " +
                   "width and height independently, like resizing any other widget.",
            Location = new Point(16, 330),
            Size = new Size(320, 34),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        Label opacityLabel = new()
        {
            Text = $"Opacity: {_canvas.BackgroundOpacityPercent}%",
            Location = new Point(16, 378),
            Size = new Size(200, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        TrackBar opacityTrack = new()
        {
            Location = new Point(16, 400),
            Size = new Size(300, 36),
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            Value = Math.Clamp(_canvas.BackgroundOpacityPercent, 0, 100)
        };

        WadevoButton resetPositionButton = new()
        {
            ButtonText = "Reset Position && Size",
            Location = new Point(16, 448),
            Size = new Size(180, 34),
            AccentColor = WadevoTheme.Colors.TextMuted
        };

        resetPositionButton.ButtonClicked += (_, _) =>
        {
            _canvas.SetBackgroundOffset(0, 0);
            _canvas.BackgroundWidthPercent = 100;
            _canvas.BackgroundHeightPercent = 100;
            _canvas.Invalidate();
        };

        void ApplyBackgroundStyle()
        {
            WadevoBackgroundScaleMode mode = sizeCombo.SelectedIndex switch
            {
                0 => WadevoBackgroundScaleMode.Fill,
                1 => WadevoBackgroundScaleMode.Fit,
                2 => WadevoBackgroundScaleMode.Stretch,
                3 => WadevoBackgroundScaleMode.Center,
                _ => WadevoBackgroundScaleMode.Fill
            };

            _canvas.SetBackgroundStyle(
                mode,
                roundedBox.Checked,
                _canvas.BackgroundWidthPercent,
                _canvas.BackgroundHeightPercent,
                opacityTrack.Value);
        }

        opacityTrack.ValueChanged += (_, _) =>
        {
            opacityLabel.Text = $"Opacity: {opacityTrack.Value}%";
            ApplyBackgroundStyle();
        };

        sizeCombo.SelectedIndexChanged += (_, _) => ApplyBackgroundStyle();
        roundedBox.CheckedChanged += (_, _) => ApplyBackgroundStyle();

        uploadButton.ButtonClicked += (_, _) =>
        {
            using OpenFileDialog dialog = new()
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif",
                Title = "Choose a background image"
            };

            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            {
                return;
            }

            _canvas.SetBackgroundImage(dialog.FileName);
            currentStatus.Text = $"Current: {Path.GetFileName(dialog.FileName)}";
        };

        clearButton.ButtonClicked += (_, _) =>
        {
            _canvas.SetBackgroundImage(null);
            currentStatus.Text = "No background set.";
        };

        content.Controls.Add(header);
        content.Controls.Add(description);
        content.Controls.Add(uploadButton);
        content.Controls.Add(clearButton);
        content.Controls.Add(currentStatus);
        content.Controls.Add(sizeLabel);
        content.Controls.Add(sizeCombo);
        content.Controls.Add(roundedBox);
        content.Controls.Add(resizeHintLabel);
        content.Controls.Add(opacityLabel);
        content.Controls.Add(opacityTrack);
        content.Controls.Add(resetPositionButton);

        WadevoDropdownPopup popup = new(content, 360, 510);
        popup.ShowBelow(anchor);
    }

    private async Task LookupArtworkAsync(
        WadevoLabeledTextBox artistBox,
        WadevoLabeledTextBox songBox,
        WadevoLabeledTextBox albumBox,
        WadevoLabeledTextBox releaseDateBox,
        WadevoButton lookupButton,
        Label lookupStatus)
    {
        lookupButton.Enabled = false;
        lookupStatus.ForeColor = WadevoTheme.Colors.TextMuted;
        lookupStatus.Text = "Looking up...";

        try
        {
            MusicMetadataModel? metadata = await MusicMetadataService.LookupAsync(
                artistBox.TextValue,
                songBox.TextValue);

            if (metadata is null)
            {
                lookupStatus.ForeColor = WadevoTheme.Colors.Warning;
                lookupStatus.Text = "No match found on iTunes.";
                return;
            }

            albumBox.TextValue = metadata.AlbumName;
            releaseDateBox.TextValue = metadata.ReleaseDate;

            _canvas.PreviewAlbum = metadata.AlbumName;
            _canvas.PreviewReleaseDate = metadata.ReleaseDate;
            _canvas.SetArtworkUrl(metadata.ArtworkUrl);

            lookupStatus.ForeColor = WadevoTheme.Colors.Success;
            lookupStatus.Text = $"Found: {metadata.AlbumName}";
        }
        catch
        {
            lookupStatus.ForeColor = WadevoTheme.Colors.Error;
            lookupStatus.Text = "Lookup failed. Check your connection.";
        }
        finally
        {
            lookupButton.Enabled = true;
        }
    }
}
