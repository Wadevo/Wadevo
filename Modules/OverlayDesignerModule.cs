namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Platforms;

public sealed class OverlayDesignerModule : WadevoModule
{
    private readonly WadevoDesignerPresetStore _presetStore = new();

    private readonly WadevoScrollablePanel _overlayListPanel = new();
    private readonly WadevoSearchBox _searchBox = new();
    private readonly Panel _stageViewport = new();
    private readonly Panel _stageContentPanel = new WadevoDoubleBufferedPanel();

    private WadevoButton? _saveButton;
    private WadevoButton? _renameButton;
    private WadevoButton? _duplicateButton;
    private WadevoButton? _copyUrlButton;
    private WadevoButton? _deleteButton;
    private WadevoButton? _backButton;
    private WadevoButton? _filterPlatformButton;

    // null = show every overlay; otherwise restrict to overlays linked to an alert whose
    // trigger belongs to this platform. Overlays not linked to any alert (Song ID,
    // Custom Overlay types) are platform-neutral and always shown regardless of this filter.
    private CommandSourcePlatform? _activePlatformFilter;

    private WadevoDesignerPresetModel? _selectedOverlay;
    private WadevoDesignerShell? _activeShell;
    private string _pendingOverlayType = "Song ID";

    private PictureBox? _slideOverlay;
    private System.Windows.Forms.Timer? _slideTimer;
    private bool _isChangingPage;

    public override string ModuleName => "Overlay Designer";
    public override string ModuleDescription => "Create and manage every overlay in one place.";

    public OverlayDesignerModule()
    {
        BackColor = WadevoTheme.Colors.Background;
        Padding = new Padding(0);
        AutoScroll = false;
        Dock = DockStyle.Fill;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        WadevoGlassCard listCard = CreateOverlayListCard();
        WadevoGlassCard studioCard = CreateStudioCard();

        listCard.Dock = DockStyle.Fill;
        studioCard.Dock = DockStyle.Fill;

        listCard.Margin = new Padding(0, 0, 16, 0);
        studioCard.Margin = new Padding(0);

        layout.Controls.Add(listCard, 0, 0);
        layout.Controls.Add(studioCard, 1, 0);

        Controls.Add(layout);

        RefreshOverlayList();
        ShowEmptyStudio();
    }

    private WadevoGlassCard CreateOverlayListCard()
    {
        WadevoGlassCard card = new()
        {
            AccentColor = WadevoTheme.Colors.Cyan
        };

        WadevoButton newButton = CreateButton("+ New Overlay", WadevoTheme.Colors.Accent);
        newButton.Location = new Point(18, 20);
        newButton.Size = new Size(196, 42);
        newButton.ButtonClicked += (_, _) => StartNewOverlay();

        Label title = new()
        {
            Text = "🎨 Saved Overlays",
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Cyan,
            Location = new Point(18, 76),
            Size = new Size(206, 30),
            BackColor = Color.Transparent
        };

        _searchBox.Location = new Point(18, 112);
        _searchBox.Size = new Size(196, 38);
        _searchBox.PlaceholderText = "Search overlays...";
        _searchBox.SearchTextChanged += (_, _) => RefreshOverlayList();

        _filterPlatformButton = new WadevoButton
        {
            ButtonText = "Filter: All Platforms ▾",
            Location = new Point(18, 156),
            Size = new Size(196, 34),
            AccentColor = WadevoTheme.Colors.TextMuted,
            Font = WadevoTheme.Fonts.Small
        };
        _filterPlatformButton.ButtonClicked += (_, _) => ShowPlatformFilterDropdown();

        _overlayListPanel.Location = new Point(18, 198);
        _overlayListPanel.Size = new Size(196, 404);
        _overlayListPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _overlayListPanel.BackColor = Color.Transparent;
        _overlayListPanel.Content.Padding = new Padding(0, 0, 0, 12);

        card.Resize += (_, _) =>
        {
            int bottomPadding = 28;
            int availableHeight = card.ClientSize.Height - _overlayListPanel.Top - bottomPadding;

            _overlayListPanel.Height = Math.Max(120, availableHeight);
            _overlayListPanel.Width = Math.Max(120, card.ClientSize.Width - 36);
        };

        card.Controls.Add(newButton);
        card.Controls.Add(title);
        card.Controls.Add(_searchBox);
        card.Controls.Add(_filterPlatformButton);
        card.Controls.Add(_overlayListPanel);

        return card;
    }

    private WadevoGlassCard CreateStudioCard()
    {
        WadevoGlassCard card = new()
        {
            AccentColor = WadevoTheme.Colors.Accent
        };

        _stageViewport.Location = new Point(30, 24);
        _stageViewport.Size = new Size(760, 676);
        _stageViewport.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _stageViewport.BackColor = Color.Transparent;

        _stageContentPanel.Location = new Point(0, 0);
        _stageContentPanel.Size = _stageViewport.Size;
        _stageContentPanel.BackColor = Color.Transparent;

        _stageViewport.Controls.Add(_stageContentPanel);

        _backButton = CreateButton("← Back to List", WadevoTheme.Colors.Cyan);
        _backButton.Size = new Size(150, 34);
        _backButton.Visible = false;
        _backButton.ButtonClicked += (_, _) => ShowEmptyStudio();

        _saveButton = CreateButton("💾 Save", WadevoTheme.Colors.Accent);
        _saveButton.Visible = false;
        _saveButton.ButtonClicked += (_, _) => SaveOverlay();

        _renameButton = CreateButton("✏️ Rename", WadevoTheme.Colors.Purple);
        _renameButton.Visible = false;
        _renameButton.ButtonClicked += (_, _) => RenameOverlay();

        _duplicateButton = CreateButton("📋 Duplicate", WadevoTheme.Colors.Cyan);
        _duplicateButton.Visible = false;
        _duplicateButton.ButtonClicked += (_, _) => DuplicateOverlay();

        _copyUrlButton = CreateButton("🔗 Copy OBS URL", WadevoTheme.Colors.Success);
        _copyUrlButton.Visible = false;
        _copyUrlButton.ButtonClicked += (_, _) => CopyOverlayUrl();

        _deleteButton = CreateButton("🗑 Delete", WadevoTheme.Colors.Error);
        _deleteButton.Visible = false;
        _deleteButton.ButtonClicked += (_, _) => DeleteOverlay();

        WadevoButton backButton = _backButton;
        WadevoButton saveButton = _saveButton;
        WadevoButton renameButton = _renameButton;
        WadevoButton duplicateButton = _duplicateButton;
        WadevoButton copyUrlButton = _copyUrlButton;
        WadevoButton deleteButton = _deleteButton;

        card.Resize += (_, _) =>
        {
            int sidePadding = 60;
            int contentBottomPadding = 70;

            _stageViewport.Width = Math.Max(320, card.ClientSize.Width - sidePadding);
            _stageViewport.Height = Math.Max(160, card.ClientSize.Height - _stageViewport.Top - contentBottomPadding);

            if (!_isChangingPage)
                _stageContentPanel.Size = _stageViewport.ClientSize;

            int buttonY = _stageViewport.Bottom + 16;
            int rightEdge = card.ClientSize.Width - 30;

            deleteButton.Location = new Point(rightEdge - deleteButton.Width, buttonY);
            duplicateButton.Location = new Point(deleteButton.Left - duplicateButton.Width - 10, buttonY);
            copyUrlButton.Location = new Point(duplicateButton.Left - copyUrlButton.Width - 10, buttonY);
            renameButton.Location = new Point(copyUrlButton.Left - renameButton.Width - 10, buttonY);
            saveButton.Location = new Point(renameButton.Left - saveButton.Width - 10, buttonY);
            backButton.Location = new Point(30, buttonY);
        };

        card.Controls.Add(_stageViewport);
        card.Controls.Add(backButton);
        card.Controls.Add(saveButton);
        card.Controls.Add(renameButton);
        card.Controls.Add(duplicateButton);
        card.Controls.Add(copyUrlButton);
        card.Controls.Add(deleteButton);

        return card;
    }

    private void ShowPlatformFilterDropdown()
    {
        if (_filterPlatformButton is null)
        {
            return;
        }

        Panel content = new()
        {
            BackColor = WadevoTheme.Colors.Background
        };

        List<(CommandSourcePlatform? Platform, string Label, Color Color)> options = new()
        {
            (null, "All Platforms", WadevoTheme.Colors.TextMuted)
        };

        foreach (PlatformDescriptor descriptor in PlatformRegistry.Implemented)
        {
            options.Add((descriptor.Platform, $"{descriptor.Name} only", descriptor.AccentColor));
        }

        int y = 8;

        foreach ((CommandSourcePlatform? platform, string label, Color color) in options)
        {
            WadevoButton optionButton = new()
            {
                ButtonText = label,
                Location = new Point(8, y),
                Size = new Size(180, 32),
                AccentColor = color
            };

            optionButton.ButtonClicked += (_, _) =>
            {
                _activePlatformFilter = platform;
                RefreshFilterPlatformLabel();
                RefreshOverlayList();
            };

            content.Controls.Add(optionButton);
            y += 38;
        }

        WadevoDropdownPopup popup = new(content, 196, y + 8);
        popup.ShowBelow(_filterPlatformButton);
    }

    private void RefreshFilterPlatformLabel()
    {
        if (_filterPlatformButton is null)
        {
            return;
        }

        if (_activePlatformFilter is null)
        {
            _filterPlatformButton.ButtonText = "Filter: All Platforms ▾";
            _filterPlatformButton.AccentColor = WadevoTheme.Colors.TextMuted;
            return;
        }

        PlatformDescriptor descriptor =
            PlatformRegistry.Get(_activePlatformFilter.Value);

        _filterPlatformButton.ButtonText = $"Filter: {descriptor.Name} only ▾";
        _filterPlatformButton.AccentColor = descriptor.AccentColor;
    }

    // Overlay presets have no platform of their own - platform only exists at the Alert
    // level (via its namespaced EventTrigger, e.g. "twitch.follow"). An overlay counts as
    // belonging to a platform only if some alert links to it via LinkedOverlayPresetId.
    private static CommandSourcePlatform? GetLinkedPlatform(string overlayPresetId)
    {
        AlertProfileModel? linkedAlert = WadevoAlertHub.ProfileService.Profiles
            .FirstOrDefault(profile => profile.LinkedOverlayPresetId == overlayPresetId);

        return linkedAlert is null
            ? null
            : PlatformRegistry.GetByTriggerName(linkedAlert.EventTrigger)?.Platform;
    }

    private void RefreshOverlayList()
    {
        _overlayListPanel.Content.SuspendLayout();

        foreach (Control control in _overlayListPanel.Content.Controls.Cast<Control>().ToList())
        {
            _overlayListPanel.Content.Controls.Remove(control);
            control.Dispose();
        }

        string search = _searchBox.SearchText.Trim().ToLowerInvariant();
        IEnumerable<WadevoDesignerPresetModel> overlays = _presetStore.LoadAll()
            .OrderByDescending(overlay => overlay.SavedAtUtc);

        if (!string.IsNullOrWhiteSpace(search))
        {
            overlays = overlays.Where(overlay =>
                overlay.Name.ToLowerInvariant().Contains(search) ||
                overlay.OverlayType.ToLowerInvariant().Contains(search));
        }

        if (_activePlatformFilter is not null)
        {
            overlays = overlays.Where(overlay =>
                GetLinkedPlatform(overlay.Id) is null ||
                GetLinkedPlatform(overlay.Id) == _activePlatformFilter);
        }

        foreach (WadevoDesignerPresetModel overlay in overlays)
        {
            WadevoOverlayListItem item = new()
            {
                OverlayName = overlay.Name,
                OverlayType = overlay.OverlayType,
                SavedAtText = overlay.SavedAtUtc.ToLocalTime().ToString("MMM d, h:mm tt"),
                Selected = overlay.Id == _selectedOverlay?.Id,
                Margin = new Padding(0, 0, 0, 10)
            };

            item.ItemClicked += (_, _) =>
            {
                foreach (Control control in _overlayListPanel.Content.Controls)
                {
                    if (control is WadevoOverlayListItem listItem)
                        listItem.Selected = false;
                }

                item.Selected = true;

                OpenExistingOverlay(overlay);
            };

            _overlayListPanel.Content.Controls.Add(item);
        }

        _overlayListPanel.Content.ResumeLayout();
        _overlayListPanel.RefreshLayout();
    }

    private void ClearStageContent()
    {
        _stageContentPanel.SuspendLayout();

        foreach (Control control in _stageContentPanel.Controls.Cast<Control>().ToList())
        {
            _stageContentPanel.Controls.Remove(control);
            control.Dispose();
        }

        _stageContentPanel.ResumeLayout();
    }

    private void ShowEmptyStudio()
    {
        _selectedOverlay = null;
        _activeShell = null;

        SetChromeVisible(back: false, save: false, rename: false, duplicate: false, delete: false);

        // A prior overlay-switch animation (AnimateStagePage) can leave this panel offset
        // to one side mid-slide, or interrupted mid-transition. Resetting both explicitly
        // here guarantees the empty-state hint is actually visible, regardless of whatever
        // state the panel was left in.
        _isChangingPage = false;
        _slideTimer?.Stop();
        _slideTimer?.Dispose();
        _slideTimer = null;

        if (_slideOverlay is not null)
        {
            _stageViewport.Controls.Remove(_slideOverlay);
            _slideOverlay.Image?.Dispose();
            _slideOverlay.Dispose();
            _slideOverlay = null;
        }

        ClearStageContent();
        _stageContentPanel.Dock = DockStyle.None;
        _stageContentPanel.Location = new Point(0, 0);
        _stageContentPanel.Size = _stageViewport.ClientSize;

        Label empty = new()
        {
            Text = "Choose a saved overlay from the list, or click + New Overlay to begin.",
            Location = new Point(0, 20),
            Size = new Size(600, 60),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _stageContentPanel.Controls.Add(empty);

        RefreshOverlayList();
    }

    private void StartNewOverlay()
    {
        _selectedOverlay = null;
        _activeShell = null;

        SetChromeVisible(back: true, save: false, rename: false, duplicate: false, delete: false);

        ShowTypeChooser(direction: 1, instant: true);
    }

    // Called from the Song ID page's "Edit This Overlay" button, to jump straight into
    // editing the currently-live overlay instead of requiring a click through the list.
    public void OpenOverlayById(string presetId)
    {
        WadevoDesignerPresetModel? overlay = _presetStore.LoadAll()
            .FirstOrDefault(preset => preset.Id == presetId);

        if (overlay is not null)
        {
            OpenExistingOverlay(overlay);
        }
    }

    private void OpenExistingOverlay(WadevoDesignerPresetModel overlay)
    {
        _selectedOverlay = overlay;

        SetChromeVisible(back: true, save: true, rename: true, duplicate: true, delete: true);

        WadevoDesignerShell shell = new();
        shell.LoadPreset(overlay);
        _activeShell = shell;

        ClearStageContent();
        _stageContentPanel.Dock = DockStyle.Fill;
        shell.Dock = DockStyle.Fill;
        _stageContentPanel.Controls.Add(shell);
    }

    private void SetChromeVisible(bool back, bool save, bool rename, bool duplicate, bool delete)
    {
        if (_backButton is not null) _backButton.Visible = back;
        if (_saveButton is not null) _saveButton.Visible = save;
        if (_renameButton is not null) _renameButton.Visible = rename;
        if (_duplicateButton is not null) _duplicateButton.Visible = duplicate;
        if (_copyUrlButton is not null) _copyUrlButton.Visible = duplicate;
        if (_deleteButton is not null) _deleteButton.Visible = delete;
    }

    private void CopyOverlayUrl()
    {
        if (_selectedOverlay is null)
        {
            return;
        }

        string url = $"{OverlayServer.BaseUrl}overlay?id={_selectedOverlay.Id}";
        Clipboard.SetText(url);

        WadevoMessageBox.Show(
            FindForm(),
            "Copied! Add this as its own Browser Source in OBS - each saved overlay has its " +
            "own unique URL now, so you can add as many at once as you want, each shown and " +
            "sized independently.\n\n" + url,
            "OBS Browser Source URL Copied");
    }

    private void RenameOverlay()
    {
        if (_selectedOverlay is null)
        {
            return;
        }

        using WadevoTextPromptForm prompt = new(
            "Rename Overlay",
            "New name:",
            _selectedOverlay.Name);

        DialogResult result = prompt.ShowDialog(FindForm());

        ForceFullRepaint();

        if (result != DialogResult.OK || string.IsNullOrWhiteSpace(prompt.InputText))
        {
            return;
        }

        _presetStore.RenamePreset(_selectedOverlay.Id, prompt.InputText);
        _selectedOverlay.Name = prompt.InputText.Trim();

        RefreshOverlayList();
    }

    private void SaveOverlay()
    {
        if (_activeShell is null)
        {
            return;
        }

        if (_selectedOverlay is not null)
        {
            _presetStore.UpdatePreset(
                _selectedOverlay.Id,
                _activeShell.GetCurrentElements(),
                _activeShell.GetStyleSettings());

            RefreshOverlayList();
            return;
        }

        using WadevoTextPromptForm prompt = new(
            "Save Overlay",
            "Name this overlay:",
            $"Overlay {DateTime.Now:MMM d, h:mm tt}");

        DialogResult result = prompt.ShowDialog(FindForm());

        ForceFullRepaint();

        if (result != DialogResult.OK || string.IsNullOrWhiteSpace(prompt.InputText))
        {
            return;
        }

        WadevoDesignerPresetModel saved = _presetStore.SavePreset(
            prompt.InputText,
            _pendingOverlayType,
            _activeShell.GetCurrentElements(),
            _activeShell.GetStyleSettings());

        _selectedOverlay = saved;
        SetChromeVisible(back: true, save: true, rename: true, duplicate: true, delete: true);

        RefreshOverlayList();
    }

    private void DuplicateOverlay()
    {
        if (_selectedOverlay is null || _activeShell is null)
        {
            return;
        }

        WadevoDesignerPresetModel duplicate = _presetStore.SavePreset(
            $"{_selectedOverlay.Name} Copy",
            _selectedOverlay.OverlayType,
            _activeShell.GetCurrentElements(),
            _activeShell.GetStyleSettings());

        RefreshOverlayList();
        OpenExistingOverlay(duplicate);
    }

    private void DeleteOverlay()
    {
        if (_selectedOverlay is null)
        {
            return;
        }

        bool confirmed = WadevoMessageBox.Confirm(
            FindForm(),
            $"Delete the overlay \"{_selectedOverlay.Name}\"? This can't be undone.",
            "Delete Overlay");

        ForceFullRepaint();

        if (!confirmed)
        {
            return;
        }

        _presetStore.DeletePreset(_selectedOverlay.Id);

        // If the deleted overlay was the one currently pushed to OBS, the live pointer
        // needs clearing too - otherwise OverlayServer keeps looking up a preset id that
        // no longer exists in the store, and falls back to whatever stale scratch document
        // happens to be lying around instead of reverting to nothing.
        LiveOverlaySettingsStore liveSettingsStore = new();

        if (liveSettingsStore.GetLivePresetId() == _selectedOverlay.Id)
        {
            liveSettingsStore.SetLivePresetId(null);
        }

        ShowEmptyStudio();
    }

    private void AddTypeCard(Control host, string icon, string title, string description, int x, int y, bool available)
    {
        WadevoSelectionCard card = new()
        {
            IconText = icon,
            TitleText = title,
            DescriptionText = available ? description : $"{description} (Coming soon)",
            Location = new Point(x, y),
            Size = new Size(210, 96)
        };

        if (available)
        {
            card.CardClicked += (_, _) => SelectOverlayType(title);
        }

        host.Controls.Add(card);
    }

    private void ShowTypeChooser(int direction, bool instant = false)
    {
        Panel typeChooser = new()
        {
            BackColor = Color.Transparent,
            Size = new Size(_stageViewport.ClientSize.Width, _stageViewport.ClientSize.Height)
        };

        Label header = new()
        {
            Text = "What kind of overlay would you like to build?",
            Location = new Point(0, 0),
            Size = new Size(700, 30),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        typeChooser.Controls.Add(header);

        AddTypeCard(typeChooser, "🎵", "Song ID", "Song, artist, album, artwork.", 0, 45, available: true);
        AddTypeCard(typeChooser, "🧩", "Custom Overlay", "Start blank and add widgets freely.", 250, 45, available: true);
        AddTypeCard(typeChooser, "💬", "Command Overlay", "On-screen command output.", 500, 45, available: false);

        if (instant)
        {
            ClearStageContent();
            _stageContentPanel.Dock = DockStyle.Fill;
            typeChooser.Dock = DockStyle.Fill;
            _stageContentPanel.Controls.Add(typeChooser);
            ForceFullRepaint();
        }
        else
        {
            AnimateStagePage(typeChooser, direction);
        }
    }

    private void SelectOverlayType(string overlayType)
    {
        _selectedOverlay = null;
        _pendingOverlayType = overlayType;

        SetChromeVisible(back: true, save: true, rename: false, duplicate: false, delete: false);

        WadevoDesignerShell newShell = new() { Dock = DockStyle.Fill };
        _activeShell = newShell;
        newShell.OverlayType = overlayType;

        // "Song ID" keeps the shell's default starting layout (Song ID/Artist/
        // Song/etc). "Custom" starts genuinely blank - previously the only way to use
        // widgets like Image/Clock/Countdown at all was through a Song ID overlay,
        // even for something that had nothing to do with music.
        if (overlayType == "Custom Overlay")
        {
            newShell.ClearToBlank();
        }

        AnimateStagePage(newShell, direction: 1);
    }

    private void ForceFullRepaint()
    {
        _stageViewport.Invalidate(true);
        _stageViewport.Update();
        _stageContentPanel.Invalidate(true);
        _stageContentPanel.Update();
    }

    private void AnimateStagePage(Control newContent, int direction)
    {
        if (_isChangingPage)
        {
            ClearStageContent();
            _stageContentPanel.Dock = DockStyle.None;
            newContent.Dock = DockStyle.Fill;
            _stageContentPanel.Controls.Add(newContent);
            ForceFullRepaint();
            return;
        }

        int viewportWidth = Math.Max(_stageViewport.ClientSize.Width, 1);
        int viewportHeight = Math.Max(_stageViewport.ClientSize.Height, 1);

        bool hasExistingContent = _stageContentPanel.Controls.Count > 0;

        if (!hasExistingContent)
        {
            ClearStageContent();
            _stageContentPanel.Dock = DockStyle.None;
            _stageContentPanel.Size = new Size(viewportWidth, viewportHeight);
            _stageContentPanel.Location = new Point(0, 0);
            newContent.Dock = DockStyle.Fill;
            _stageContentPanel.Controls.Add(newContent);
            ForceFullRepaint();
            return;
        }

        _isChangingPage = true;

        Bitmap snapshot = new(viewportWidth, viewportHeight);
        _stageContentPanel.DrawToBitmap(snapshot, new Rectangle(Point.Empty, new Size(viewportWidth, viewportHeight)));

        _slideOverlay?.Dispose();
        _slideOverlay = new PictureBox
        {
            Image = snapshot,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Size = new Size(viewportWidth, viewportHeight),
            Location = new Point(0, 0)
        };

        _stageViewport.Controls.Add(_slideOverlay);
        _slideOverlay.BringToFront();

        ClearStageContent();
        _stageContentPanel.Dock = DockStyle.None;
        newContent.Dock = DockStyle.Fill;
        _stageContentPanel.Controls.Add(newContent);

        _stageContentPanel.Size = new Size(viewportWidth, viewportHeight);
        _stageContentPanel.Location = new Point(viewportWidth * direction, 0);

        int elapsed = 0;
        const int durationMs = 260;
        const int intervalMs = 15;

        _slideTimer?.Stop();
        _slideTimer?.Dispose();
        _slideTimer = new System.Windows.Forms.Timer { Interval = intervalMs };

        _slideTimer.Tick += (_, _) =>
        {
            elapsed += intervalMs;
            double t = Math.Min(1.0, elapsed / (double)durationMs);
            double eased = 1 - Math.Pow(1 - t, 3);

            _stageContentPanel.Left = (int)(viewportWidth * direction * (1 - eased));

            if (_slideOverlay is not null)
                _slideOverlay.Left = (int)(-viewportWidth * direction * eased);

            if (t >= 1.0)
            {
                _slideTimer?.Stop();
                _slideTimer?.Dispose();
                _slideTimer = null;

                _stageContentPanel.Left = 0;

                if (_slideOverlay is not null)
                {
                    _stageViewport.Controls.Remove(_slideOverlay);
                    _slideOverlay.Image?.Dispose();
                    _slideOverlay.Dispose();
                    _slideOverlay = null;
                }

                _isChangingPage = false;

                ForceFullRepaint();
            }
        };

        _slideTimer.Start();
    }

    private static WadevoButton CreateButton(string text, Color color)
    {
        return new WadevoButton
        {
            ButtonText = text,
            AccentColor = color,
            Size = new Size(130, 34)
        };
    }
}
