namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Platforms;

public sealed class AlertsModule : WadevoModule
{
    private readonly AlertProfileStore _alertStore = new();
    private readonly WadevoSearchBox _searchBox = new();

    private readonly HashSet<string> _activeTriggerFilters = new();
    private WadevoButton? _filterTriggerButton;

    private static readonly (string Trigger, string Icon, string Label)[] FilterableTriggers =
        PlatformRegistry.AllAlertTriggers().ToArray();

    private readonly WadevoScrollablePanel _alertListPanel = new();
    private readonly Panel _stageViewport = new();
    private readonly Panel _stageContentPanel = new WadevoDoubleBufferedPanel();

    private WadevoButton? _saveButton;
    private WadevoButton? _testButton;
    private WadevoButton? _duplicateButton;
    private WadevoButton? _deleteButton;
    private WadevoButton? _backButton;

    private AlertProfileModel? _selectedAlert;
    private AlertStudioControl? _activeStudio;

    public override string ModuleName => "Alerts";
    public override string ModuleDescription => "Create and manage custom on-stream alerts.";

    public AlertsModule()
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

        WadevoGlassCard listCard = CreateAlertListCard();
        WadevoGlassCard studioCard = CreateStudioCard();

        listCard.Dock = DockStyle.Fill;
        studioCard.Dock = DockStyle.Fill;

        listCard.Margin = new Padding(0, 0, 16, 0);
        studioCard.Margin = new Padding(0);

        layout.Controls.Add(listCard, 0, 0);
        layout.Controls.Add(studioCard, 1, 0);

        Controls.Add(layout);

        RefreshAlertList();
        ShowEmptyStudio();
    }

    private WadevoGlassCard CreateAlertListCard()
    {
        WadevoGlassCard card = new()
        {
            AccentColor = WadevoTheme.Colors.Purple
        };

        WadevoButton newButton = CreateButton("+ New Alert", WadevoTheme.Colors.Accent);
        newButton.Location = new Point(18, 20);
        newButton.Size = new Size(196, 42);
        newButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        newButton.ButtonClicked += (_, _) => StartNewAlert();

        Label title = new()
        {
            Text = "🚨 Saved Alerts",
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Purple,
            Location = new Point(18, 76),
            Size = new Size(206, 30),
            BackColor = Color.Transparent
        };

        _searchBox.Location = new Point(18, 112);
        _searchBox.Size = new Size(196, 38);
        _searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _searchBox.PlaceholderText = "Search alerts...";
        _searchBox.SearchTextChanged += (_, _) => RefreshAlertList();

        _filterTriggerButton = new WadevoButton
        {
            ButtonText = "Filter: All Triggers ▾",
            Location = new Point(18, 156),
            Size = new Size(196, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AccentColor = WadevoTheme.Colors.TextMuted
        };

        _filterTriggerButton.ButtonClicked += (_, _) => ShowFilterDropdown();

        _alertListPanel.Location = new Point(18, 202);
        _alertListPanel.Size = new Size(196, 400);
        _alertListPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _alertListPanel.BackColor = Color.Transparent;
        _alertListPanel.Content.Padding = new Padding(0, 0, 0, 12);

        card.Resize += (_, _) =>
        {
            int bottomPadding = 28;
            int availableHeight = card.ClientSize.Height - _alertListPanel.Top - bottomPadding;

            newButton.Width = Math.Max(120, card.ClientSize.Width - 36);
            _searchBox.Width = Math.Max(120, card.ClientSize.Width - 36);
            if (_filterTriggerButton is not null)
                _filterTriggerButton.Width = Math.Max(120, card.ClientSize.Width - 36);
            _alertListPanel.Height = Math.Max(120, availableHeight);
            _alertListPanel.Width = Math.Max(120, card.ClientSize.Width - 36);

            int itemWidth = Math.Max(120, _alertListPanel.ClientSize.Width - 20);

            foreach (Control control in _alertListPanel.Content.Controls)
            {
                control.Width = itemWidth;
            }
        };

        card.Controls.Add(newButton);
        card.Controls.Add(title);
        card.Controls.Add(_searchBox);
        if (_filterTriggerButton is not null)
            card.Controls.Add(_filterTriggerButton);
        card.Controls.Add(_alertListPanel);

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

        _stageContentPanel.Dock = DockStyle.Fill;
        _stageContentPanel.BackColor = Color.Transparent;

        _stageViewport.Controls.Add(_stageContentPanel);

        _backButton = CreateButton("← Back to List", WadevoTheme.Colors.Cyan);
        _backButton.Size = new Size(150, 34);
        _backButton.Visible = false;
        _backButton.ButtonClicked += (_, _) => ShowEmptyStudio();

        _saveButton = CreateButton("💾 Save", WadevoTheme.Colors.Accent);
        _saveButton.Visible = false;
        _saveButton.ButtonClicked += (_, _) => SaveAlert();

        _testButton = CreateButton("▶ Test", WadevoTheme.Colors.Cyan);
        _testButton.Visible = false;
        _testButton.ButtonClicked += (_, _) => TestAlert();

        _duplicateButton = CreateButton("📋 Duplicate", WadevoTheme.Colors.Cyan);
        _duplicateButton.Visible = false;
        _duplicateButton.ButtonClicked += (_, _) => DuplicateAlert();

        _deleteButton = CreateButton("🗑 Delete", WadevoTheme.Colors.Error);
        _deleteButton.Visible = false;
        _deleteButton.ButtonClicked += (_, _) => DeleteAlert();

        WadevoButton backButton = _backButton;
        WadevoButton saveButton = _saveButton;
        WadevoButton testButton = _testButton;
        WadevoButton duplicateButton = _duplicateButton;
        WadevoButton deleteButton = _deleteButton;

        card.Resize += (_, _) =>
        {
            int sidePadding = 60;
            int contentBottomPadding = 70;

            _stageViewport.Width = Math.Max(320, card.ClientSize.Width - sidePadding);
            _stageViewport.Height = Math.Max(160, card.ClientSize.Height - _stageViewport.Top - contentBottomPadding);

            int buttonY = _stageViewport.Bottom + 16;
            int rightEdge = card.ClientSize.Width - 30;

            deleteButton.Location = new Point(rightEdge - deleteButton.Width, buttonY);
            duplicateButton.Location = new Point(deleteButton.Left - duplicateButton.Width - 10, buttonY);
            testButton.Location = new Point(duplicateButton.Left - testButton.Width - 10, buttonY);
            saveButton.Location = new Point(testButton.Left - saveButton.Width - 10, buttonY);
            backButton.Location = new Point(30, buttonY);
        };

        card.Controls.Add(_stageViewport);
        card.Controls.Add(backButton);
        card.Controls.Add(saveButton);
        card.Controls.Add(testButton);
        card.Controls.Add(duplicateButton);
        card.Controls.Add(deleteButton);

        return card;
    }

    private void RefreshAlertList()
    {
        _alertListPanel.Content.SuspendLayout();

        foreach (Control control in _alertListPanel.Content.Controls.Cast<Control>().ToList())
        {
            _alertListPanel.Content.Controls.Remove(control);
            control.Dispose();
        }

        string search = _searchBox.SearchText.Trim().ToLowerInvariant();
        IEnumerable<AlertProfileModel> alerts = _alertStore.LoadAll();

        if (!string.IsNullOrWhiteSpace(search))
        {
            alerts = alerts.Where(alert =>
                alert.Name.ToLowerInvariant().Contains(search) ||
                alert.EventTrigger.ToLowerInvariant().Contains(search));
        }

        if (_activeTriggerFilters.Count > 0)
        {
            alerts = alerts.Where(alert => _activeTriggerFilters.Contains(alert.EventTrigger));
        }

        alerts = alerts.OrderByDescending(alert => alert.CreatedAt);

        int itemWidth = Math.Max(120, _alertListPanel.ClientSize.Width - 20);

        foreach (AlertProfileModel alert in alerts)
        {
            WadevoCommandListItem item = new()
            {
                CommandName = string.IsNullOrWhiteSpace(alert.Name) ? "Untitled Alert" : alert.Name,
                Trigger = string.IsNullOrWhiteSpace(alert.EventTrigger) ? "Manual only" : alert.EventTrigger,
                CommandType = string.IsNullOrWhiteSpace(alert.LinkedOverlayPresetId) ? "⚠ Not designed" : "✓ Designed",
                EnabledCommand = alert.IsEnabled,
                Selected = alert.Id == _selectedAlert?.Id,
                Width = itemWidth,
                Margin = new Padding(0, 0, 0, 10)
            };

            item.ItemClicked += (_, _) => OpenExistingAlert(alert);

            _alertListPanel.Content.Controls.Add(item);
        }

        _alertListPanel.Content.ResumeLayout();
        _alertListPanel.RefreshLayout();
    }

    private void ShowFilterDropdown()
    {
        if (_filterTriggerButton is null)
        {
            return;
        }

        Panel content = new()
        {
            BackColor = WadevoTheme.Colors.Background
        };

        WadevoButton allPlatformsButton = new()
        {
            ButtonText = "All",
            Location = new Point(12, 8),
            Size = new Size(60, 28),
            AccentColor = WadevoTheme.Colors.TextMuted
        };

        allPlatformsButton.ButtonClicked += (_, _) =>
        {
            _activeTriggerFilters.Clear();
            RefreshFilterTriggerLabel();
            RefreshAlertList();
            ShowFilterDropdown();
        };

        content.Controls.Add(allPlatformsButton);

        int quickFilterX = 78;

        foreach (PlatformDescriptor platformDescriptor in PlatformRegistry.Implemented)
        {
            string[] platformTriggers = FilterableTriggers
                .Where(t => platformDescriptor.OwnsTriggerName(t.Trigger))
                .Select(t => t.Trigger)
                .ToArray();

            WadevoButton platformOnlyButton = new()
            {
                ButtonText = $"{platformDescriptor.Name} only",
                Location = new Point(quickFilterX, 8),
                Size = new Size(90, 28),
                AccentColor = platformDescriptor.AccentColor
            };

            platformOnlyButton.ButtonClicked += (_, _) =>
            {
                _activeTriggerFilters.Clear();
                foreach (string trigger in platformTriggers) _activeTriggerFilters.Add(trigger);
                RefreshFilterTriggerLabel();
                RefreshAlertList();
                ShowFilterDropdown();
            };

            content.Controls.Add(platformOnlyButton);
            quickFilterX += 96;
        }

        Panel divider = new()
        {
            Location = new Point(12, 44),
            Size = new Size(Math.Max(252, quickFilterX - 12), 1),
            BackColor = Color.FromArgb(70, WadevoTheme.Colors.Accent)
        };
        content.Controls.Add(divider);

        int y = 56;

        foreach ((string trigger, string icon, string label) in FilterableTriggers)
        {
            WadevoCheckBox checkBox = new()
            {
                Text = $"{icon}  {label}",
                Location = new Point(12, y),
                Size = new Size(190, 26),
                Font = WadevoTheme.Fonts.Default,
                ForeColor = WadevoTheme.Colors.Text,
                Checked = _activeTriggerFilters.Contains(trigger)
            };

            checkBox.CheckedChanged += (_, _) =>
            {
                if (checkBox.Checked)
                {
                    _activeTriggerFilters.Add(trigger);
                }
                else
                {
                    _activeTriggerFilters.Remove(trigger);
                }

                RefreshFilterTriggerLabel();
                RefreshAlertList();
            };

            content.Controls.Add(checkBox);
            y += 30;
        }

        int popupWidth = Math.Max(220, quickFilterX + 12);
        WadevoDropdownPopup popup = new(content, popupWidth, y + 16);
        popup.ShowBelow(_filterTriggerButton);
    }

    private void RefreshFilterTriggerLabel()
    {
        if (_filterTriggerButton is null)
        {
            return;
        }

        int activeCount = _activeTriggerFilters.Count;

        _filterTriggerButton.ButtonText = activeCount == 0
            ? "Filter: All Triggers ▾"
            : $"Filter: {activeCount} active ▾";

        _filterTriggerButton.AccentColor = activeCount == 0
            ? WadevoTheme.Colors.TextMuted
            : WadevoTheme.Colors.Accent;
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
        _selectedAlert = null;
        _activeStudio = null;

        SetChromeVisible(back: false, save: false, duplicate: false, delete: false);

        ClearStageContent();

        Label empty = new()
        {
            Text = "Choose a saved alert from the list, or click + New Alert to begin.",
            Location = new Point(0, 20),
            Size = new Size(600, 60),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _stageContentPanel.Controls.Add(empty);

        RefreshAlertList();
    }

    private void StartNewAlert()
    {
        _selectedAlert = null;

        SetChromeVisible(back: true, save: true, duplicate: false, delete: false);

        AlertStudioControl studio = new(new AlertProfileModel { Name = "" });
        _activeStudio = studio;

        ClearStageContent();
        studio.Dock = DockStyle.Fill;
        _stageContentPanel.Controls.Add(studio);
    }

    private void OpenExistingAlert(AlertProfileModel alert)
    {
        _selectedAlert = alert;

        SetChromeVisible(back: true, save: true, duplicate: true, delete: true);

        AlertStudioControl studio = new(alert);
        _activeStudio = studio;

        ClearStageContent();
        studio.Dock = DockStyle.Fill;
        _stageContentPanel.Controls.Add(studio);

        RefreshAlertList();
    }

    private void SetChromeVisible(bool back, bool save, bool duplicate, bool delete)
    {
        if (_backButton is not null) _backButton.Visible = back;
        if (_saveButton is not null) _saveButton.Visible = save;
        if (_testButton is not null) _testButton.Visible = save;
        if (_duplicateButton is not null) _duplicateButton.Visible = duplicate;
        if (_deleteButton is not null) _deleteButton.Visible = delete;
    }

    private void TestAlert()
    {
        if (_activeStudio is null)
        {
            return;
        }

        _activeStudio.SaveEmbeddedDesign();
        WadevoAlertHub.TriggerPreview(_activeStudio.GetProfile());
    }

    private void SaveAlert()
    {
        if (_activeStudio is null)
        {
            return;
        }

        _activeStudio.SaveEmbeddedDesign();
        AlertProfileModel profile = _activeStudio.GetProfile();

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            WadevoMessageBox.Show(FindForm(), "Give this alert a name before saving.", "Name Required");
            return;
        }

        if (_selectedAlert is not null)
        {
            profile.Id = _selectedAlert.Id;
            _alertStore.Update(profile);
        }
        else
        {
            _alertStore.SaveNew(profile);
        }

        _selectedAlert = profile;
        SetChromeVisible(back: true, save: true, duplicate: true, delete: true);

        RefreshAlertList();
    }

    private void DuplicateAlert()
    {
        if (_selectedAlert is null || _activeStudio is null)
        {
            return;
        }

        AlertProfileModel copy = _activeStudio.GetProfile();
        copy.Id = Guid.NewGuid().ToString("N");
        copy.Name = $"{copy.Name} Copy";

        // A genuinely independent copy of the design too, not a second alert pointing at
        // the same one - editing the duplicate's appearance later shouldn't silently
        // change the original's.
        copy.LinkedOverlayPresetId = _activeStudio.DuplicateEmbeddedDesign(copy.Name);

        _alertStore.SaveNew(copy);

        RefreshAlertList();
        OpenExistingAlert(copy);
    }

    private void DeleteAlert()
    {
        if (_selectedAlert is null)
        {
            return;
        }

        bool confirmed = WadevoMessageBox.Confirm(
            FindForm(),
            $"Delete the alert \"{_selectedAlert.Name}\"? This can't be undone.",
            "Delete Alert");

        if (!confirmed)
        {
            return;
        }

        _alertStore.Delete(_selectedAlert.Id);
        ShowEmptyStudio();
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
