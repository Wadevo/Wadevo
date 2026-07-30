namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Modules;
using Wadevo.Modules.Commands;
using Wadevo.Modules.OverlayEngine;
using Wadevo.Services;
using System.Drawing.Drawing2D;

public class WadevoShell : UserControl
{
    private readonly Panel _sidebarPanel;
    private readonly Panel _topPanel;
    private readonly Panel _contentPanel;
    private readonly Panel _statusPanel;
    private readonly WadevoLogo _brandLogo;
    private readonly Label _taglineLabel;
    private readonly Label _pageTitleLabel;
    private readonly WadevoTitleImage _pageTitleImage;
    private readonly Label _pageSubtitleLabel;
    private readonly Label _statusLabel;
    private readonly NowPlayingCard _nowPlayingCard;
    private readonly List<Button> _navButtons = new();
    private Label? _seratoStatusValueLabel;

    // Sidebar is a fixed 280px wide with 30px/26px left/right padding (224px available),
    // minus the 10px the themed scrollbar reserves on the right once the nav list is
    // tall enough to scroll. Buttons are sized to this explicitly because FlowLayoutPanel
    // (used for the scrollable nav list) ignores Dock/Anchor on its children entirely.
    private const int SidebarNavWidth = 214;

    public WadevoShell()
    {
        Dock = DockStyle.Fill;
        BackColor = WadevoTheme.Colors.Background;
        Font = WadevoTheme.Fonts.Default;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 280,
            BackColor = Color.Black,
            Padding = new Padding(30, 28, 26, 24)
        };

        _topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 96,
            BackColor = WadevoTheme.Colors.Background,
            Padding = new Padding(42, 10, 42, 4)
        };

        _statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            BackColor = WadevoTheme.Colors.BackgroundSoft,
            Padding = new Padding(42, 8, 42, 4)
        };

        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = WadevoTheme.Colors.Background,
            Padding = new Padding(32, 0, 32, 24),
            AutoScroll = true
        };

        _brandLogo = new WadevoLogo
        {
            LogoFile = "small wadevo logo.png",
            Height = 130,
            Dock = DockStyle.Top
        };

        _taglineLabel = new Label
        {
            Height = 0,
            Visible = false
        };

        _pageTitleLabel = new Label
        {
            Text = "Dashboard",
            Height = 48,
            Dock = DockStyle.Top,
            Font = WadevoTheme.Fonts.Hero,
            ForeColor = WadevoTheme.Colors.Accent,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _pageTitleImage = new WadevoTitleImage
        {
            Height = 48,
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
            Visible = false
        };

        _pageSubtitleLabel = new Label
        {
            Text = "Create, control, and polish your stream experience.",
            Height = 30,
            Dock = DockStyle.Top,
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _statusLabel = new Label
        {
            Text = "● Wadevo starting",
            Dock = DockStyle.Fill,
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Success,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _nowPlayingCard = new NowPlayingCard();

        Controls.Add(_contentPanel);
        Controls.Add(_statusPanel);
        Controls.Add(_topPanel);
        Controls.Add(_sidebarPanel);

        BuildSidebar();
        BuildTopPanel();
        BuildStatusPanel();
        ShowDashboard();
    }

    public void SetNowPlaying(string artist, string title)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new MethodInvoker(() => SetNowPlaying(artist, title)));
            return;
        }

        _nowPlayingCard.SetSong(artist, title);
        ApplySeratoStatusToCard();
    }

    public void SetStatus(string status, Color color)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new MethodInvoker(() => SetStatus(status, color)));
            return;
        }

        _statusLabel.Text = status;
        _statusLabel.ForeColor = color;
    }

    private void BuildSidebar()
    {
        WadevoScrollablePanel navScrollPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        navScrollPanel.Content.FlowDirection = FlowDirection.TopDown;
        navScrollPanel.Content.WrapContents = false;
        navScrollPanel.Content.Padding = new Padding(0);

        navScrollPanel.Content.Controls.Add(CreateSpacer(28));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.GettingStarted, "Getting Started", ShowGettingStarted));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.Dashboard, "Dashboard", ShowDashboard));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.Connections, "Connections", ShowConnections));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.Donations, "Support Wadevo", ShowDonations));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.OverlayDesigner, "Overlay Designer", ShowOverlayDesigner));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.OverlayEngine, "Overlay Engine", ShowOverlayEngine));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.Alerts, "Alerts", ShowAlerts));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.Commands, "Commands", ShowCommands));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.Soundboard, "Soundboard", ShowSoundboard));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.Gifs, "GIFs", ShowGifs));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.NowPlaying, "The Booth", ShowTheBooth));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.SongRequests, "Song Requests", ShowSongRequests));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.AssetLibrary, "Asset Library", ShowAssetLibrary));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.WorkspaceStudio, "Workspace Studio", OpenWorkspaceStudio));
        navScrollPanel.Content.Controls.Add(CreateNavButton(WadevoIconKind.Settings, "Settings", ShowSettings));
        navScrollPanel.Content.Controls.Add(CreateSpacer(8));

        // Logo stays pinned at the top, in its original spot, while the nav list below
        // it scrolls independently in the remaining space.
        _taglineLabel.Dock = DockStyle.Top;

        _sidebarPanel.Controls.Add(navScrollPanel);
        _sidebarPanel.Controls.Add(_taglineLabel);
        _sidebarPanel.Controls.Add(_brandLogo);
    }

    private void BuildTopPanel()
    {
        _topPanel.Controls.Add(_pageSubtitleLabel);
        _topPanel.Controls.Add(_pageTitleLabel);
        _topPanel.Controls.Add(_pageTitleImage);
    }

    private void BuildStatusPanel()
    {
        _statusPanel.Controls.Add(_statusLabel);
    }

    // Every page-switch method in this class was clearing _contentPanel without disposing
    // the outgoing page first. Controls.Clear() only removes the parent/child relationship;
    // it does not dispose the removed controls. That meant every single navigation between
    // sidebar tabs left the entire previous page's control tree alive in memory - including
    // custom double-buffered controls with their own window handles - never released. This
    // is the shared fix, used by every ShowXxx() method below instead of calling
    // _contentPanel.Controls.Clear() directly.
    private void ClearContentPanel()
    {
        _pageTitleImage.Visible = false;
        _pageTitleLabel.Visible = true;

        foreach (Control control in _contentPanel.Controls.Cast<Control>().ToList())
        {
            _contentPanel.Controls.Remove(control);
            control.Dispose();
        }
    }

    // Swaps the page title from plain text to a custom-designed image, matching the same
    // hand-decorated brand style as the sidebar logo. Call this AFTER ClearContentPanel(),
    // since that resets every page back to text mode first.
    private void SetPageTitleImage(string fileName)
    {
        try
        {
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets", "Titles", fileName);

            if (!File.Exists(path))
            {
                return;
            }

            _pageTitleImage.LoadAndCrop(path);
            _pageTitleImage.Visible = true;
            _pageTitleLabel.Visible = false;
        }
        catch
        {
            // If the image can't load, the plain text title (already showing) stays as-is.
        }
    }

    private readonly WadevoDesignerPresetStore _liveOverlayPresetStore = new();
    private readonly LiveOverlaySettingsStore _liveOverlaySettingsStore = new();
    private WadevoComboBox? _liveOverlayCombo;
    private Label? _liveOverlayIdentityLabel;
    private List<WadevoDesignerPresetModel> _liveOverlayChoices = new();

    private void ShowTheBooth()
    {
        SelectNavButton("The Booth");

        _pageSubtitleLabel.Text = "Pick what's live, check connection status, jump to editing.";

        ClearContentPanel();
        SetPageTitleImage("TheBoothTitle.png");

        WadevoGlassCard liveOverlayCard = new()
        {
            Dock = DockStyle.Top,
            Height = 150,
            AccentColor = WadevoTheme.Colors.Accent
        };

        Label liveOverlayHeader = new()
        {
            Text = "🔴 Live Overlay",
            Location = new Point(24, 16),
            Size = new Size(400, 26),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        // Shows which overlay is actually live rather than a generic "Live Overlay"
        // label - the whole point of this card is knowing *what's* live, not just that
        // something is.
        _liveOverlayIdentityLabel = liveOverlayHeader;

        Label liveOverlayDescription = new()
        {
            Text = "This is what's actually showing in OBS right now.",
            Location = new Point(24, 46),
            Size = new Size(500, 22),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoComboBox liveOverlayCombo = new()
        {
            Location = new Point(24, 78),
            Size = new Size(280, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = WadevoTheme.Fonts.Default,
            DisplayMember = nameof(WadevoDesignerPresetModel.Name)
        };

        _liveOverlayCombo = liveOverlayCombo;

        WadevoButton editOverlayButton = new()
        {
            ButtonText = "✏️ Edit This Overlay",
            Location = new Point(316, 78),
            Size = new Size(180, 30),
            AccentColor = WadevoTheme.Colors.Cyan
        };

        editOverlayButton.ButtonClicked += (_, _) =>
        {
            if (liveOverlayCombo.SelectedItem is WadevoDesignerPresetModel selected)
            {
                ShowOverlayDesigner();
                (_contentPanel.Controls.OfType<OverlayDesignerModule>().FirstOrDefault())
                    ?.OpenOverlayById(selected.Id);
            }
        };

        liveOverlayCombo.SelectedIndexChanged += (_, _) =>
        {
            if (liveOverlayCombo.SelectedItem is WadevoDesignerPresetModel selected)
            {
                _liveOverlaySettingsStore.SetLivePresetId(selected.Id);

                if (_liveOverlayIdentityLabel is not null)
                {
                    _liveOverlayIdentityLabel.Text = $"🔴 {selected.Name}";
                }
            }
        };

        liveOverlayCard.Controls.Add(liveOverlayHeader);
        liveOverlayCard.Controls.Add(liveOverlayDescription);
        liveOverlayCard.Controls.Add(liveOverlayCombo);
        liveOverlayCard.Controls.Add(editOverlayButton);

        WadevoGlassCard urlCard = new()
        {
            Dock = DockStyle.Top,
            Height = 100,
            AccentColor = WadevoTheme.Colors.Purple,
            Margin = new Padding(0, 16, 0, 0)
        };

        Label urlHeader = new()
        {
            Text = "🔗 OBS Browser Source URL",
            Location = new Point(24, 16),
            Size = new Size(300, 26),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Purple,
            BackColor = Color.Transparent
        };

        TextBox urlBox = new()
        {
            Location = new Point(24, 50),
            Size = new Size(400, 26),
            Text = OverlayServer.NowPlayingUrl,
            ReadOnly = true,
            Font = WadevoTheme.Fonts.Default,
            BackColor = WadevoTheme.Colors.BackgroundSoft,
            ForeColor = WadevoTheme.Colors.Text,
            BorderStyle = BorderStyle.FixedSingle
        };

        WadevoButton copyUrlButton = new()
        {
            ButtonText = "📋 Copy",
            Location = new Point(436, 48),
            Size = new Size(90, 30),
            AccentColor = WadevoTheme.Colors.Accent
        };

        copyUrlButton.ButtonClicked += (_, _) => Clipboard.SetText(OverlayServer.NowPlayingUrl);

        urlCard.Controls.Add(urlHeader);
        urlCard.Controls.Add(urlBox);
        urlCard.Controls.Add(copyUrlButton);

        FlowLayoutPanel statusRow = new()
        {
            Dock = DockStyle.Top,
            Height = 120,
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 16, 0, 0)
        };

        WadevoAppSettingsModel boothDjSettings = new WadevoAppSettingsStore().Load();
        bool boothUsingVirtualDj = boothDjSettings.DjSoftware.Equals("VirtualDJ", StringComparison.OrdinalIgnoreCase);
        bool boothUsingSeratoLocal = !boothUsingVirtualDj && boothDjSettings.SeratoReadMethod.Equals("LocalHistoryFile", StringComparison.OrdinalIgnoreCase);

        string boothDjLabel = boothUsingVirtualDj ? "VIRTUALDJ" : boothUsingSeratoLocal ? "SERATO (LOCAL)" : "SERATO DJ PRO";

        statusRow.Controls.Add(CreateStatusCard("♫", boothDjLabel, "Checking", WadevoTheme.Colors.Warning, out _seratoStatusValueLabel));
        statusRow.Controls.Add(CreateStatusCard("▣", "OVERLAY ENGINE", "Running", WadevoTheme.Colors.Cyan, out _));
        statusRow.Controls.Add(CreateStatusCard("◉", "OBS STUDIO", "Browser source", WadevoTheme.Colors.Purple, out _));

        _contentPanel.Controls.Add(_nowPlayingCard);
        _contentPanel.Controls.Add(CreateSpacer(20));
        _contentPanel.Controls.Add(statusRow);
        _contentPanel.Controls.Add(urlCard);
        _contentPanel.Controls.Add(liveOverlayCard);

        RefreshLiveOverlayChoices();
        ApplySeratoStatusToCard();

        _statusLabel.Text = boothUsingVirtualDj ? "● Checking VirtualDJ history" : boothUsingSeratoLocal ? "● Checking Serato local history" : "● Checking Serato playlist";
        _statusLabel.ForeColor = WadevoTheme.Colors.Warning;
    }

    private void ApplySeratoStatusToCard()
    {
        if (_seratoStatusValueLabel is null)
        {
            return;
        }

        WadevoAppSettingsModel djSettings = new WadevoAppSettingsStore().Load();
        bool usingVirtualDj = djSettings.DjSoftware.Equals("VirtualDJ", StringComparison.OrdinalIgnoreCase);
        bool isConnected = usingVirtualDj ? WadevoLiveStatus.IsVirtualDjConnected : WadevoLiveStatus.IsSeratoConnected;

        if (isConnected)
        {
            _seratoStatusValueLabel.Text = "Connected";
            _seratoStatusValueLabel.ForeColor = WadevoTheme.Colors.Success;
        }
        else
        {
            _seratoStatusValueLabel.Text = "Waiting";
            _seratoStatusValueLabel.ForeColor = WadevoTheme.Colors.Warning;
        }
    }

    private void RefreshLiveOverlayChoices()
    {
        if (_liveOverlayCombo is null)
        {
            return;
        }

        _liveOverlayChoices = _liveOverlayPresetStore.LoadAll();
        string? livePresetId = _liveOverlaySettingsStore.GetLivePresetId();

        _liveOverlayCombo.DataSource = null;
        _liveOverlayCombo.DataSource = _liveOverlayChoices;
        _liveOverlayCombo.DisplayMember = nameof(WadevoDesignerPresetModel.Name);

        if (livePresetId is not null)
        {
            int index = _liveOverlayChoices.FindIndex(preset => preset.Id == livePresetId);

            if (index >= 0)
            {
                _liveOverlayCombo.SelectedIndex = index;

                if (_liveOverlayIdentityLabel is not null)
                {
                    _liveOverlayIdentityLabel.Text = $"🔴 {_liveOverlayChoices[index].Name}";
                }

                return;
            }
        }

        if (_liveOverlayIdentityLabel is not null)
        {
            _liveOverlayIdentityLabel.Text = _liveOverlayChoices.Count == 0
                ? "🔴 No saved overlays yet"
                : "🔴 No overlay selected";
        }
    }

    private void ShowSettings()
    {
        SelectNavButton("Settings");

        _pageSubtitleLabel.Text = "App-wide preferences and connections.";

        ClearContentPanel();
        SetPageTitleImage("SettingsTitle.png");

        SettingsModule settingsModule = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(settingsModule);

        _statusLabel.Text = "● Settings ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private WorkspaceStudioForm? _workspaceStudioForm;

    private void OpenWorkspaceStudio()
    {
        if (_workspaceStudioForm is { IsDisposed: false })
        {
            _workspaceStudioForm.WindowState = FormWindowState.Normal;
            _workspaceStudioForm.BringToFront();
            _workspaceStudioForm.Activate();
            return;
        }

        _workspaceStudioForm = new WorkspaceStudioForm();
        _workspaceStudioForm.Show();
    }

    private void ShowDashboard()
    {
        SelectNavButton("Dashboard");

        _pageSubtitleLabel.Text = "Today's activity, at a glance.";

        ClearContentPanel();
        SetPageTitleImage("DashboardTitle.png");

        DashboardModule dashboardModule = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(dashboardModule);

        _statusLabel.Text = "● Dashboard ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void ShowAssetLibrary()
    {
        SelectNavButton("Asset Library");

        _pageSubtitleLabel.Text = "Everything you've uploaded, in one place.";

        ClearContentPanel();
        SetPageTitleImage("AssetLibraryTitle.png");

        AssetLibraryModule assetLibraryModule = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(assetLibraryModule);

        _statusLabel.Text = "● Asset Library ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void ShowDonations()
    {
        SelectNavButton("Support Wadevo");

        _pageSubtitleLabel.Text = "Free to use. Support is always welcome, never required.";

        ClearContentPanel();
        SetPageTitleImage("SupportWadevoTitle.png");

        DonationsModule donationsModule = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(donationsModule);

        _statusLabel.Text = "● Ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void ShowSongRequests()
    {
        SelectNavButton("Song Requests");

        _pageSubtitleLabel.Text = "Let chat request songs, manage the queue live.";

        ClearContentPanel();
        SetPageTitleImage("SongRequestsTitle.png");

        SongRequestsModule songRequestsModule = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(songRequestsModule);

        _statusLabel.Text = "● Song Requests ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void ShowGettingStarted()
    {
        SelectNavButton("Getting Started");

        _pageSubtitleLabel.Text = "New here? Start with these steps.";

        ClearContentPanel();
        SetPageTitleImage("GettingStartedTitle.png");

        GettingStartedModule gettingStartedModule = new()
        {
            Dock = DockStyle.Fill
        };

        gettingStartedModule.OpenRequested += GettingStartedModule_OpenRequested;

        _contentPanel.Controls.Add(gettingStartedModule);

        _statusLabel.Text = "● Getting Started ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void GettingStartedModule_OpenRequested(string pageName)
    {
        switch (pageName)
        {
            case "Connections":
                ShowConnections();
                break;

            case "Settings":
                ShowSettings();
                break;

            case "GIFs":
                ShowGifs();
                break;

            case "Commands":
                ShowCommands();
                break;

            case "Overlay Engine":
                ShowOverlayEngine();
                break;
        }
    }

    private void ShowConnections()
    {
        SelectNavButton("Connections");

        _pageSubtitleLabel.Text = "Connect once. Every Wadevo feature can use it.";

        ClearContentPanel();
        SetPageTitleImage("ConnectionsTitle.png");

        ConnectionsHubModule connectionsModule = new()
        {
            Dock = DockStyle.Fill
        };

        connectionsModule.OpenRequested += ConnectionsModule_OpenRequested;

        _contentPanel.Controls.Add(connectionsModule);

        _statusLabel.Text = "● Connections Hub ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void ConnectionsModule_OpenRequested(string connectionName)
    {
        switch (connectionName)
        {
            case "Blaze":
                using (BlazeConnectionPopupForm blazePopup = new())
                {
                    blazePopup.ShowDialog(this);
                }
                break;

            case "Twitch":
                using (TwitchConnectionPopupForm twitchPopup = new())
                {
                    twitchPopup.ShowDialog(this);
                }
                break;

            case "OBS Studio":
                using (ObsConnectionPopupForm obsPopup = new())
                {
                    obsPopup.ShowDialog(this);
                }
                break;

            case "Serato":
            case "VirtualDJ":
                ShowTheBooth();
                break;

            case "Overlay Engine":
                ShowOverlayEngine();
                break;
        }
    }

    private void ShowGifs()
    {
        SelectNavButton("GIFs");

        _pageSubtitleLabel.Text = "Search Giphy, then send a GIF straight to your stream.";

        ClearContentPanel();
        SetPageTitleImage("GifsTitle.png");

        GifsModule gifsModule = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(gifsModule);

        _statusLabel.Text = "● GIFs ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void ShowSoundboard()
    {
        SelectNavButton("Soundboard");

        _pageTitleLabel.Text = "Soundboard";
        _pageSubtitleLabel.Text = "Import local sounds and bind them to hotkeys you can hit mid-game.";

        ClearContentPanel();
        SetPageTitleImage("SoundboardTitle.png");

        SoundboardModule soundboardModule = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(soundboardModule);

        _statusLabel.Text = "● Soundboard ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void ShowCommands()
    {
        SelectNavButton("Commands");

        _pageSubtitleLabel.Text = "Create chat commands for messages, GIFs, overlays, sounds, and automations.";

        ClearContentPanel();
        SetPageTitleImage("CommandsTitle.png");

        CommandsModule commandsModule = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(commandsModule);

        _statusLabel.Text = "● Commands ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void ShowAlerts()
    {
        SelectNavButton("Alerts");

        _pageSubtitleLabel.Text = "Create and manage custom on-stream alerts.";

        ClearContentPanel();
        SetPageTitleImage("AlertsTitle.png");

        AlertsModule alertsModule = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(alertsModule);

        _statusLabel.Text = "● Alerts ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void ShowOverlayEngine()
    {
        SelectNavButton("Overlay Engine");

        _pageSubtitleLabel.Text = "Manage browser overlays for OBS. Copy overlay URLs, monitor status, and build your stream.";

        ClearContentPanel();
        SetPageTitleImage("OverlayEngineTitle.png");

        OverlayEngineModule module = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(module);

        _statusLabel.Text = "● Overlay Engine ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private void ShowPlaceholder(string title)
    {
        SelectNavButton(title);

        _pageTitleLabel.Text = title;
        _pageSubtitleLabel.Text = "This module is coming soon.";

        ClearContentPanel();

        Label label = new()
        {
            Text = $"{title} module coming soon.",
            Dock = DockStyle.Top,
            Height = 60,
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.TextSecondary,
            BackColor = Color.Transparent
        };

        _contentPanel.Controls.Add(label);

        _statusLabel.Text = $"● {title} coming soon";
        _statusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
    }

    private void SelectNavButton(string pageName)
    {
        foreach (Button button in _navButtons)
        {
            bool selected = button.Text.Contains(pageName);

            button.Font = selected ? WadevoTheme.Fonts.Bold : WadevoTheme.Fonts.Medium;
            button.ForeColor = selected ? WadevoTheme.Colors.Text : WadevoTheme.Colors.TextSecondary;
            button.BackColor = Color.Black;

            if (button is GlowNavButton glowButton)
                glowButton.Selected = selected;

            button.Invalidate();
        }
    }

    private static WadevoGlassCard CreateStatusCard(string icon, string title, string value, Color accentColor, out Label valueLabel)
    {
        WadevoGlassCard card = new()
        {
            Width = 240,
            Height = 96,
            Margin = new Padding(0, 0, 20, 0),
            AccentColor = accentColor
        };

        card.Controls.Add(new Label
        {
            Text = icon,
            Font = new Font("Segoe UI", 28F, FontStyle.Bold),
            ForeColor = accentColor,
            Location = new Point(20, 24),
            Size = new Size(50, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        });

        card.Controls.Add(new Label
        {
            Text = title,
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.TextSecondary,
            Location = new Point(82, 22),
            Size = new Size(140, 28),
            BackColor = Color.Transparent
        });

        valueLabel = new Label
        {
            Text = value,
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = accentColor,
            Location = new Point(82, 50),
            Size = new Size(140, 32),
            BackColor = Color.Transparent
        };

        card.Controls.Add(valueLabel);

        return card;
    }

    private Button CreateNavButton(WadevoIconKind iconKind, string text, Action onClick)
    {
        GlowNavButton button = new()
        {
            Text = text,
            IconKind = iconKind,
            Width = SidebarNavWidth,
            Height = 50,
            Margin = new Padding(0),
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.TextSecondary,
            BackColor = Color.Black,
            UseVisualStyleBackColor = false,
            TabStop = false,
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.Black;
        button.FlatAppearance.MouseDownBackColor = Color.Black;
        button.FlatAppearance.CheckedBackColor = Color.Black;

        button.Click += (_, _) => onClick();

        _navButtons.Add(button);

        return button;
    }

    private static Button CreateActionButton(
        string text,
        Point location,
        Size size,
        Color accentColor)
    {
        Button button = new()
        {
            Text = text,
            Location = location,
            Size = size,
            FlatStyle = FlatStyle.Flat,
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = accentColor,
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }

    private static Panel CreateSpacer(int height)
    {
        return new Panel
        {
            Width = SidebarNavWidth,
            Height = height,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
    }

    private void ShowOverlayDesigner()
    {
        SelectNavButton("Overlay Designer");

        _pageSubtitleLabel.Text = "Create and manage every overlay in one place.";

        ClearContentPanel();
        SetPageTitleImage("OverlayDesignerTitle.png");

        OverlayDesignerModule overlayDesignerModule = new()
        {
            Dock = DockStyle.Fill
        };

        _contentPanel.Controls.Add(overlayDesignerModule);

        _statusLabel.Text = "● Overlay Designer ready";
        _statusLabel.ForeColor = WadevoTheme.Colors.Success;
    }

    private sealed class GlowNavButton : Button
    {
        public bool Selected { get; set; }

        public WadevoIconKind IconKind { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.Black);

            string label = Text ?? string.Empty;

            Color iconColor = Selected
                ? WadevoTheme.Colors.Accent
                : WadevoTheme.Colors.Cyan;

            Color textColor = Selected
                ? WadevoTheme.Colors.Text
                : WadevoTheme.Colors.TextSecondary;

            Font textFont = Selected
                ? WadevoTheme.Fonts.Bold
                : WadevoTheme.Fonts.Medium;

            Rectangle iconBounds = new(16, 0, 24, Height);
            Rectangle textBounds = new(50, 0, Width - 56, Height);

            TextFormatFlags flags =
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.Left |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.SingleLine;

            // Soft glow behind the icon: two low-alpha offset passes, then the crisp icon on top.
            WadevoIconRenderer.Draw(
                e.Graphics,
                IconKind,
                new Rectangle(iconBounds.Left - 1, iconBounds.Top, iconBounds.Width, iconBounds.Height),
                Color.FromArgb(80, iconColor));

            WadevoIconRenderer.Draw(
                e.Graphics,
                IconKind,
                new Rectangle(iconBounds.Left + 1, iconBounds.Top, iconBounds.Width, iconBounds.Height),
                Color.FromArgb(80, iconColor));

            WadevoIconRenderer.Draw(e.Graphics, IconKind, iconBounds, iconColor);

            TextRenderer.DrawText(
                e.Graphics,
                label,
                textFont,
                textBounds,
                textColor,
                flags);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using LinearGradientBrush brush = new(
            ClientRectangle,
            WadevoTheme.Colors.Background,
            WadevoTheme.Colors.BackgroundSoft,
            LinearGradientMode.ForwardDiagonal);

        e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}
