namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Blaze;
using Wadevo.Services.Twitch;

public sealed class DashboardModule : WadevoModule
{
    // Both top-row cards (Blaze Channel and Recent Activity) share this height so they
    // line up evenly side by side, instead of Recent Activity stretching to fill all
    // available vertical space while Blaze Channel stays a fixed, shorter height.
    private const int BlazeCardHeight = 680;
    private const int TwitchCardHeight = 460;
    private const int CardMargin = 16;
    private const int TopRowCardHeight = BlazeCardHeight + CardMargin + TwitchCardHeight;

    private readonly DashboardStatsService _statsService = WadevoDashboardHub.StatsService;

    private readonly BlazeChannelService _blazeChannelService = new();
    private readonly BlazeCategoryService _blazeCategoryService = new();
    private readonly Dictionary<string, int> _categoryNameToId = new(StringComparer.OrdinalIgnoreCase);
    private int? _gameDirectoryId;
    private readonly Dictionary<string, int> _subCategoryNameToId = new(StringComparer.OrdinalIgnoreCase);
    private bool _categoriesLoaded;
    private readonly System.Windows.Forms.Timer _blazeRefreshTimer = new() { Interval = 30_000 };

    private readonly Label _blazeStatusBadge = new();
    private readonly Label _blazeViewersValue = new();
    private readonly Label _blazeFollowersValue = new();
    private readonly Label _blazeSubscribersValue = new();

    private readonly WadevoTextBox _streamTitleTextBox = new();
    private readonly WadevoComboBox _categoryComboBox = new();
    private readonly Label _categoryLabel = new();
    private readonly WadevoTextBox _gameSearchTextBox = new();
    private readonly WadevoComboBox _gameResultsComboBox = new();
    private readonly WadevoComboBox _languageTextBox = new();
    private readonly WadevoButton _updateStreamInfoButton = new();
    private readonly Label _streamInfoStatusLabel = new();

    private readonly TwitchChannelService _twitchChannelService = new();
    private readonly TwitchCategoryService _twitchCategoryService = new();
    private List<TwitchCategoryModel> _twitchCategorySearchResults = new();
    private readonly System.Windows.Forms.Timer _twitchRefreshTimer = new() { Interval = 30_000 };

    private readonly Label _twitchStatusBadge = new();
    private readonly Label _twitchViewersValue = new();
    private readonly Label _twitchFollowersValue = new();
    private readonly Label _twitchSubscribersValue = new();

    private readonly WadevoTextBox _twitchStreamTitleTextBox = new();
    private readonly WadevoTextBox _twitchCategorySearchTextBox = new();
    private readonly WadevoComboBox _twitchCategoryResultsComboBox = new();
    private readonly WadevoButton _twitchUpdateStreamInfoButton = new();
    private readonly Label _twitchStreamInfoStatusLabel = new();

    private readonly WadevoScrollablePanel _activityPanel = new();
    private readonly Panel _statsGrid = new();

    public DashboardModule()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        _statsService.StatsChanged += (_, _) =>
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(RefreshAll));
            }
            else
            {
                RefreshAll();
            }
        };

        WadevoGlassCard combinedCard = BuildBlazeChannelCard();
        WadevoGlassCard twitchCard = BuildTwitchChannelCard();
        twitchCard.Location = new Point(0, BlazeCardHeight + CardMargin);

        Panel leftColumn = new()
        {
            Dock = DockStyle.Left,
            Width = 660,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 16, 0)
        };

        leftColumn.Controls.Add(combinedCard);
        leftColumn.Controls.Add(twitchCard);

        WadevoGlassCard activityCard = new()
        {
            Dock = DockStyle.Fill,
            AccentColor = WadevoTheme.Colors.Purple,
            Padding = new Padding(24)
        };

        Label activityHeader = new()
        {
            Text = "⚡ Recent Activity",
            Location = new Point(24, 16),
            Size = new Size(300, 28),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Purple,
            BackColor = Color.Transparent
        };

        _activityPanel.Location = new Point(24, 58);
        _activityPanel.BackColor = Color.Transparent;
        _activityPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        activityCard.Resize += (_, _) =>
        {
            _activityPanel.Width = Math.Max(120, activityCard.ClientSize.Width - 48);
            _activityPanel.Height = Math.Max(120, activityCard.ClientSize.Height - 82);
        };

        activityCard.Controls.Add(activityHeader);
        activityCard.Controls.Add(_activityPanel);

        // Both cards live in one fixed-height row, and that row is the single child of a
        // themed WadevoScrollablePanel - one green scrollbar for the whole page, scrolling
        // both cards together, instead of the previous two independent native (untheme,
        // gray) scrollbars that could scroll different things depending on which one was
        // dragged and ended up dragging both at once in a confusing way.
        Panel row = new()
        {
            Height = TopRowCardHeight,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        row.Controls.Add(activityCard);
        row.Controls.Add(leftColumn);

        WadevoScrollablePanel pageScroll = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        pageScroll.Content.FlowDirection = FlowDirection.TopDown;
        pageScroll.Content.WrapContents = false;
        pageScroll.Content.Padding = new Padding(0);

        void SyncRowWidth()
        {
            // FlowLayoutPanel (used internally by WadevoScrollablePanel) doesn't stretch
            // its children to fill available width on its own - this keeps the row's
            // width in sync with however wide the scrollable viewport currently is,
            // matching the pattern already used for the Activity feed's own inner panel.
            row.Width = Math.Max(400, pageScroll.ClientSize.Width - 16);
        }

        pageScroll.Resize += (_, _) => SyncRowWidth();
        SyncRowWidth();

        pageScroll.Content.Controls.Add(row);

        Controls.Add(pageScroll);

        _blazeRefreshTimer.Tick += (_, _) => _ = RefreshBlazeChannelAsync();
        _blazeRefreshTimer.Start();

        Disposed += (_, _) => _blazeRefreshTimer.Dispose();

        _twitchRefreshTimer.Tick += (_, _) => _ = RefreshTwitchChannelAsync();
        _twitchRefreshTimer.Start();

        Disposed += (_, _) => _twitchRefreshTimer.Dispose();

        RefreshAll();
        _ = RefreshBlazeChannelAsync();
        _ = LoadCategoriesAsync();
        _ = RefreshTwitchChannelAsync();
    }

    private WadevoGlassCard BuildBlazeChannelCard()
    {
        WadevoGlassCard card = new()
        {
            Dock = DockStyle.None,
            Location = new Point(0, 0),
            Size = new Size(644, BlazeCardHeight),
            AccentColor = WadevoTheme.Colors.Warning,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = "🔥 Blaze Channel",
            Location = new Point(24, 16),
            Size = new Size(220, 28),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Warning,
            BackColor = Color.Transparent
        };

        _blazeStatusBadge.Text = "● Checking…";
        _blazeStatusBadge.Location = new Point(250, 18);
        _blazeStatusBadge.Size = new Size(160, 24);
        _blazeStatusBadge.Font = WadevoTheme.Fonts.Small;
        _blazeStatusBadge.ForeColor = WadevoTheme.Colors.TextMuted;
        _blazeStatusBadge.BackColor = Color.Transparent;

        Panel statsRow = new()
        {
            Location = new Point(24, 50),
            Size = new Size(596, 50),
            BackColor = Color.Transparent
        };

        statsRow.Controls.Add(CreateBlazeStatTile("Viewers", _blazeViewersValue, 0));
        statsRow.Controls.Add(CreateBlazeStatTile("Followers", _blazeFollowersValue, 170));
        statsRow.Controls.Add(CreateBlazeStatTile("Subscribers", _blazeSubscribersValue, 340));

        Panel dividerOne = CreateDivider(108);

        WadevoGlassCard streamInfoCard = new()
        {
            Location = new Point(24, 120),
            Size = new Size(596, 300),
            AccentColor = WadevoTheme.Colors.Cyan,
            Padding = new Padding(20)
        };

        Label streamInfoHeader = new()
        {
            Text = "📝 Stream Info",
            Location = new Point(20, 14),
            Size = new Size(300, 26),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        _streamTitleTextBox.PlaceholderText = "Enter your stream title…";
        _streamTitleTextBox.Location = new Point(20, 48);
        _streamTitleTextBox.Size = new Size(556, 32);

        _categoryLabel.Text = "Directory";
        _categoryLabel.Location = new Point(20, 90);
        _categoryLabel.Size = new Size(220, 18);
        _categoryLabel.Font = WadevoTheme.Fonts.Small;
        _categoryLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _categoryLabel.BackColor = Color.Transparent;

        Label languageLabel = new()
        {
            Text = "Language",
            Location = new Point(292, 90),
            Size = new Size(80, 18),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _categoryComboBox.Location = new Point(20, 110);
        _categoryComboBox.Size = new Size(260, 32);
        _categoryComboBox.Font = WadevoTheme.Fonts.Default;
        _categoryComboBox.ForeColor = WadevoTheme.Colors.Text;
        _categoryComboBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _categoryComboBox.FlatStyle = FlatStyle.Flat;
        _categoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _categoryComboBox.TextAlign = ContentAlignment.MiddleCenter;
        _categoryComboBox.Items.Add("Connect Blaze to load categories…");
        _categoryComboBox.SelectedIndex = 0;

        _languageTextBox.Items.AddRange(BlazeLanguageOptions.Common.Select(l => (object)l.Code).ToArray());
        _languageTextBox.Text = "en";
        _languageTextBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageTextBox.TextAlign = ContentAlignment.MiddleCenter;
        _languageTextBox.Location = new Point(292, 110);
        _languageTextBox.Size = new Size(60, 32);
        _languageTextBox.Font = WadevoTheme.Fonts.Default;
        _languageTextBox.ForeColor = WadevoTheme.Colors.Text;
        _languageTextBox.BackColor = WadevoTheme.Colors.BackgroundSoft;

        // Blaze's own "Edit Stream Info" only drills into a second picker for the Game
        // directory - other directories (Music, IRL, Art, etc.) are used directly as the
        // category with no further breakdown. This mirrors that: hidden until a directory
        // that actually has sub-categories is selected.
        Label subCategoryLabel = new()
        {
            Text = "Game (search by name)",
            Location = new Point(20, 150),
            Size = new Size(260, 18),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent,
            Visible = false
        };

        // Editable and search-driven rather than a pre-loaded list - matches Blaze's own
        // "Edit Stream Info" dialog, which uses a live search box for Game rather than a
        // dropdown, and sidesteps needing to fetch potentially hundreds of games up front.
        // A plain typing box here (not an editable combo) is what actually makes centered
        // text possible - a standalone TextBox supports real alignment; an editable
        // combo's text area is a native control that doesn't reliably take styling changes
        // applied after the fact.
        _gameSearchTextBox.PlaceholderText = "Type a game name…";
        _gameSearchTextBox.Location = new Point(20, 168);
        _gameSearchTextBox.Size = new Size(196, 38);
        _gameSearchTextBox.TextAlign = HorizontalAlignment.Center;
        _gameSearchTextBox.Visible = false;

        WadevoButton searchGameButton = new()
        {
            ButtonText = "🔍 Search",
            Location = new Point(226, 170),
            Size = new Size(130, 36),
            AccentColor = WadevoTheme.Colors.Cyan,
            Visible = false
        };

        // Results picker - a DropDownList, same reliable owner-drawn approach as Directory
        // above, shown once a search actually returns matches to choose from.
        _gameResultsComboBox.Location = new Point(20, 210);
        _gameResultsComboBox.Size = new Size(336, 32);
        _gameResultsComboBox.Font = WadevoTheme.Fonts.Default;
        _gameResultsComboBox.ForeColor = WadevoTheme.Colors.Text;
        _gameResultsComboBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _gameResultsComboBox.FlatStyle = FlatStyle.Flat;
        _gameResultsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _gameResultsComboBox.TextAlign = ContentAlignment.MiddleCenter;
        _gameResultsComboBox.Visible = false;

        searchGameButton.ButtonClicked += (_, _) => _ = SearchGamesAsync();
        _gameSearchTextBox.InnerKeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = SearchGamesAsync();
            }
        };

        _categoryComboBox.SelectedIndexChanged += (_, _) =>
        {
            ApplyDirectorySelection(subCategoryLabel);
            searchGameButton.Visible = _gameSearchTextBox.Visible;
        };

        _updateStreamInfoButton.ButtonText = "Update";
        _updateStreamInfoButton.Location = new Point(364, 106);
        _updateStreamInfoButton.Size = new Size(130, 36);
        _updateStreamInfoButton.ButtonClicked += (_, _) => _ = UpdateStreamInfoAsync();

        _streamInfoStatusLabel.Location = new Point(20, 250);
        _streamInfoStatusLabel.Size = new Size(556, 18);
        _streamInfoStatusLabel.Font = WadevoTheme.Fonts.Small;
        _streamInfoStatusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _streamInfoStatusLabel.BackColor = Color.Transparent;
        _streamInfoStatusLabel.Text = "";

        streamInfoCard.Controls.Add(streamInfoHeader);
        streamInfoCard.Controls.Add(_streamTitleTextBox);
        streamInfoCard.Controls.Add(_categoryLabel);
        streamInfoCard.Controls.Add(languageLabel);
        streamInfoCard.Controls.Add(_categoryComboBox);
        streamInfoCard.Controls.Add(_languageTextBox);
        streamInfoCard.Controls.Add(subCategoryLabel);
        streamInfoCard.Controls.Add(_gameSearchTextBox);
        streamInfoCard.Controls.Add(searchGameButton);
        streamInfoCard.Controls.Add(_gameResultsComboBox);
        streamInfoCard.Controls.Add(_updateStreamInfoButton);
        streamInfoCard.Controls.Add(_streamInfoStatusLabel);

        // Vote Reminder used to live here as its own bespoke toggle. Removed in favor of
        // Timed Commands (Commands tab) - the same "remind viewers every N minutes" behavior
        // is now just a Timer-mode command with a chat-message response, one general
        // mechanism instead of a one-off feature.
        Panel dividerTwo = CreateDivider(432);

        Label statsHeader = new()
        {
            Text = "📊 Today",
            Location = new Point(24, 444),
            Size = new Size(300, 30),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent
        };

        _statsGrid.Location = new Point(24, 480);
        _statsGrid.Size = new Size(596, 160);
        _statsGrid.BackColor = Color.Transparent;

        card.Controls.Add(header);
        card.Controls.Add(_blazeStatusBadge);
        card.Controls.Add(statsRow);
        card.Controls.Add(dividerOne);
        card.Controls.Add(streamInfoCard);
        card.Controls.Add(dividerTwo);
        card.Controls.Add(statsHeader);
        card.Controls.Add(_statsGrid);

        return card;
    }

    private WadevoGlassCard BuildTwitchChannelCard()
    {
        WadevoGlassCard card = new()
        {
            Dock = DockStyle.None,
            Size = new Size(644, TwitchCardHeight),
            AccentColor = WadevoTheme.Colors.Purple,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = "🟣 Twitch Channel",
            Location = new Point(24, 16),
            Size = new Size(220, 28),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Purple,
            BackColor = Color.Transparent
        };

        _twitchStatusBadge.Text = "● Checking…";
        _twitchStatusBadge.Location = new Point(250, 18);
        _twitchStatusBadge.Size = new Size(180, 24);
        _twitchStatusBadge.Font = WadevoTheme.Fonts.Small;
        _twitchStatusBadge.ForeColor = WadevoTheme.Colors.TextMuted;
        _twitchStatusBadge.BackColor = Color.Transparent;

        Panel statsRow = new()
        {
            Location = new Point(24, 50),
            Size = new Size(596, 50),
            BackColor = Color.Transparent
        };

        statsRow.Controls.Add(CreateTwitchStatTile("Viewers", _twitchViewersValue, 0));
        statsRow.Controls.Add(CreateTwitchStatTile("Followers", _twitchFollowersValue, 170));
        statsRow.Controls.Add(CreateTwitchStatTile("Subscribers", _twitchSubscribersValue, 340));

        Panel dividerOne = new()
        {
            Location = new Point(24, 108),
            Size = new Size(596, 1),
            BackColor = Color.FromArgb(70, WadevoTheme.Colors.Purple)
        };

        WadevoGlassCard streamInfoCard = new()
        {
            Location = new Point(24, 120),
            Size = new Size(596, 280),
            AccentColor = WadevoTheme.Colors.Cyan,
            Padding = new Padding(20)
        };

        Label streamInfoHeader = new()
        {
            Text = "📝 Stream Info",
            Location = new Point(20, 14),
            Size = new Size(300, 26),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        _twitchStreamTitleTextBox.PlaceholderText = "Enter your stream title…";
        _twitchStreamTitleTextBox.Location = new Point(20, 48);
        _twitchStreamTitleTextBox.Size = new Size(556, 32);

        Label categoryLabel = new()
        {
            Text = "Category",
            Location = new Point(20, 90),
            Size = new Size(220, 18),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _twitchCategorySearchTextBox.PlaceholderText = "Type a category/game name…";
        _twitchCategorySearchTextBox.Location = new Point(20, 110);
        _twitchCategorySearchTextBox.Size = new Size(336, 32);

        WadevoButton searchCategoryButton = new()
        {
            ButtonText = "🔍 Search",
            Location = new Point(366, 110),
            Size = new Size(130, 32),
            AccentColor = WadevoTheme.Colors.Cyan
        };

        _twitchCategoryResultsComboBox.Location = new Point(20, 152);
        _twitchCategoryResultsComboBox.Size = new Size(476, 32);
        _twitchCategoryResultsComboBox.Font = WadevoTheme.Fonts.Default;
        _twitchCategoryResultsComboBox.ForeColor = WadevoTheme.Colors.Text;
        _twitchCategoryResultsComboBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _twitchCategoryResultsComboBox.FlatStyle = FlatStyle.Flat;
        _twitchCategoryResultsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _twitchCategoryResultsComboBox.Items.Add("Leave category unchanged");
        _twitchCategoryResultsComboBox.SelectedIndex = 0;

        searchCategoryButton.ButtonClicked += (_, _) => _ = SearchTwitchCategoriesAsync();
        _twitchCategorySearchTextBox.InnerKeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = SearchTwitchCategoriesAsync();
            }
        };

        _twitchUpdateStreamInfoButton.ButtonText = "Update";
        _twitchUpdateStreamInfoButton.Location = new Point(20, 196);
        _twitchUpdateStreamInfoButton.Size = new Size(130, 36);
        _twitchUpdateStreamInfoButton.ButtonClicked += (_, _) => _ = UpdateTwitchStreamInfoAsync();

        _twitchStreamInfoStatusLabel.Location = new Point(160, 202);
        _twitchStreamInfoStatusLabel.Size = new Size(416, 24);
        _twitchStreamInfoStatusLabel.Font = WadevoTheme.Fonts.Small;
        _twitchStreamInfoStatusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _twitchStreamInfoStatusLabel.BackColor = Color.Transparent;
        _twitchStreamInfoStatusLabel.Text = "";

        streamInfoCard.Controls.Add(streamInfoHeader);
        streamInfoCard.Controls.Add(_twitchStreamTitleTextBox);
        streamInfoCard.Controls.Add(categoryLabel);
        streamInfoCard.Controls.Add(_twitchCategorySearchTextBox);
        streamInfoCard.Controls.Add(searchCategoryButton);
        streamInfoCard.Controls.Add(_twitchCategoryResultsComboBox);
        streamInfoCard.Controls.Add(_twitchUpdateStreamInfoButton);
        streamInfoCard.Controls.Add(_twitchStreamInfoStatusLabel);

        card.Controls.Add(header);
        card.Controls.Add(_twitchStatusBadge);
        card.Controls.Add(statsRow);
        card.Controls.Add(dividerOne);
        card.Controls.Add(streamInfoCard);

        return card;
    }

    private static Panel CreateTwitchStatTile(string label, Label valueLabel, int x)
    {
        Panel tile = new()
        {
            Location = new Point(x, 0),
            Size = new Size(160, 50),
            BackColor = Color.Transparent
        };

        valueLabel.Text = "—";
        valueLabel.Location = new Point(0, 0);
        valueLabel.Size = new Size(160, 30);
        valueLabel.Font = WadevoTheme.Fonts.CardHeader;
        valueLabel.ForeColor = WadevoTheme.Colors.Purple;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.BackColor = Color.Transparent;

        Label captionLabel = new()
        {
            Text = label,
            Location = new Point(0, 30),
            Size = new Size(160, 18),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        tile.Controls.Add(valueLabel);
        tile.Controls.Add(captionLabel);

        return tile;
    }

    private async Task RefreshTwitchChannelAsync()
    {
        TwitchAuthenticationService auth = TwitchAuthenticationService.Shared;

        if (!auth.IsAuthenticated || string.IsNullOrWhiteSpace(auth.Connection.UserId))
        {
            SetTwitchStatus("○ Not connected — see Connections tab", WadevoTheme.Colors.TextMuted);
            return;
        }

        try
        {
            TwitchChannelStatsModel stats = await _twitchChannelService.GetStatsAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Connection.UserId);

            _twitchViewersValue.Text = stats.IsLive ? stats.ViewerCount.ToString("N0") : "—";
            _twitchFollowersValue.Text = stats.FollowerCount.ToString("N0");
            _twitchSubscribersValue.Text = stats.SubscriberCount.ToString("N0");

            if (!_twitchStreamTitleTextBox.Focused && string.IsNullOrEmpty(_twitchStreamTitleTextBox.Text))
            {
                _twitchStreamTitleTextBox.Text = stats.Title;
            }

            SetTwitchStatus(
                stats.IsLive ? "🔴 Live" : "○ Connected — offline",
                stats.IsLive ? WadevoTheme.Colors.Error : WadevoTheme.Colors.Success);
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Twitch dashboard refresh failed: {ex.Message}");
            SetTwitchStatus("● Couldn't reach Twitch", WadevoTheme.Colors.Error);
        }
    }

    private async Task SearchTwitchCategoriesAsync()
    {
        TwitchAuthenticationService auth = TwitchAuthenticationService.Shared;

        if (!auth.IsAuthenticated)
        {
            _twitchStreamInfoStatusLabel.Text = "Connect Twitch first (Connections tab).";
            return;
        }

        string query = _twitchCategorySearchTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        try
        {
            _twitchStreamInfoStatusLabel.Text = "Searching…";

            _twitchCategorySearchResults = (await _twitchCategoryService.SearchCategoriesAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                query)).ToList();

            _twitchCategoryResultsComboBox.Items.Clear();
            _twitchCategoryResultsComboBox.Items.Add("Leave category unchanged");

            foreach (TwitchCategoryModel category in _twitchCategorySearchResults)
            {
                _twitchCategoryResultsComboBox.Items.Add(category.Name);
            }

            _twitchCategoryResultsComboBox.SelectedIndex = 0;

            _twitchStreamInfoStatusLabel.Text = _twitchCategorySearchResults.Count > 0
                ? $"Found {_twitchCategorySearchResults.Count} match(es) — pick one below."
                : "No matching categories found.";
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Twitch category search failed: {ex.Message}");
            _twitchStreamInfoStatusLabel.Text = "Couldn't search categories - try again.";
        }
    }

    private async Task UpdateTwitchStreamInfoAsync()
    {
        TwitchAuthenticationService auth = TwitchAuthenticationService.Shared;

        if (!auth.IsAuthenticated || string.IsNullOrWhiteSpace(auth.Connection.UserId))
        {
            _twitchStreamInfoStatusLabel.Text = "Connect Twitch first (Connections tab).";
            return;
        }

        string? categoryId = null;

        if (_twitchCategoryResultsComboBox.SelectedIndex > 0)
        {
            int selectedResultIndex = _twitchCategoryResultsComboBox.SelectedIndex - 1;

            if (selectedResultIndex >= 0 && selectedResultIndex < _twitchCategorySearchResults.Count)
            {
                categoryId = _twitchCategorySearchResults[selectedResultIndex].Id;
            }
        }

        try
        {
            _twitchUpdateStreamInfoButton.Enabled = false;
            _twitchStreamInfoStatusLabel.Text = "Updating…";

            await _twitchChannelService.UpdateChannelInfoAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Connection.UserId,
                _twitchStreamTitleTextBox.Text.Trim(),
                categoryId);

            _twitchStreamInfoStatusLabel.Text = "✅ Updated.";
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Twitch update channel info failed: {ex.Message}");
            _twitchStreamInfoStatusLabel.Text = "Couldn't update - try again.";
        }
        finally
        {
            _twitchUpdateStreamInfoButton.Enabled = true;
        }
    }

    private void SetTwitchStatus(string text, Color color)
    {
        _twitchStatusBadge.Text = text;
        _twitchStatusBadge.ForeColor = color;
    }

    private static Panel CreateDivider(int y)
    {
        return new Panel
        {
            Location = new Point(24, y),
            Size = new Size(596, 1),
            BackColor = Color.FromArgb(70, WadevoTheme.Colors.Warning)
        };
    }

    private static Panel CreateBlazeStatTile(string label, Label valueLabel, int x)
    {
        Panel tile = new()
        {
            Location = new Point(x, 0),
            Size = new Size(160, 50),
            BackColor = Color.Transparent
        };

        valueLabel.Text = "—";
        valueLabel.Location = new Point(0, 0);
        valueLabel.Size = new Size(160, 28);
        valueLabel.Font = WadevoTheme.Fonts.CardHeader;
        valueLabel.ForeColor = WadevoTheme.Colors.Text;
        valueLabel.BackColor = Color.Transparent;

        Label nameLabel = new()
        {
            Text = label,
            Location = new Point(0, 30),
            Size = new Size(160, 18),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        tile.Controls.Add(valueLabel);
        tile.Controls.Add(nameLabel);

        return tile;
    }

    private async Task RefreshBlazeChannelAsync()
    {
        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        if (!auth.IsAuthenticated)
        {
            SetBlazeStatus("○ Not connected — see Connections tab", WadevoTheme.Colors.TextMuted);
            return;
        }

        // Categories only load successfully once Blaze is authenticated. If the Dashboard
        // was already open when the connection happened (rather than being freshly opened
        // afterward), the one-shot load at construction time would have failed and never
        // been retried - this periodic refresh, which already runs every 30s regardless,
        // is a natural place to catch that instead of requiring a tab switch to recover.
        if (!_categoriesLoaded)
        {
            _ = LoadCategoriesAsync();
        }

        try
        {
            BlazeChannelStatsModel stats = await _blazeChannelService.GetStatsAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Settings.ClientSecret);

            BlazeLiveStatsModel liveStats = await _blazeChannelService.GetLiveStatsAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Settings.ClientSecret);

            if (IsDisposed)
            {
                return;
            }

            _blazeViewersValue.Text = liveStats.ViewerCount.ToString();
            _blazeFollowersValue.Text = stats.FollowerCount.ToString();
            _blazeSubscribersValue.Text = stats.SubscriberCount.ToString();

            SetBlazeStatus(
                liveStats.IsLive ? "● LIVE" : "○ Offline",
                liveStats.IsLive ? WadevoTheme.Colors.Success : WadevoTheme.Colors.TextMuted);
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Blaze dashboard refresh failed: {ex.Message}");
            SetBlazeStatus("● Couldn't reach Blaze", WadevoTheme.Colors.Error);
        }
    }

    private static readonly string[] KnownDirectoryNames =
        { "Game", "IRL", "Music", "Art", "Crypto", "Gambling & Casino" };

    private async Task LoadCategoriesAsync()
    {
        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        if (!auth.IsAuthenticated)
        {
            return;
        }

        try
        {
            _categoryNameToId.Clear();
            _categoryComboBox.Items.Clear();
            _gameDirectoryId = null;

            // Search for each known directory by name instead of trying to browse/paginate
            // the whole catalog and hope they show up - multiple attempts at that only ever
            // surfaced "Gambling & Casino" out of a 100-row response. Searching by name is
            // also literally what Blaze's own "Edit Stream Info" dialog does for its Game
            // field (it's a search box, not a dropdown).
            foreach (string name in KnownDirectoryNames)
            {
                // "Game" specifically has failed to match under its own name across several
                // attempts (label shown in Blaze's picker isn't necessarily the literal
                // category name) - trying a few likely spellings covers that without
                // guessing endlessly one at a time.
                string[] termsToTry = name == "Game" ? new[] { "Game", "Games", "Gaming" } : new[] { name };
                BlazeCategoryModel? match = null;

                foreach (string term in termsToTry)
                {
                    IReadOnlyList<BlazeCategoryModel> results = await _blazeCategoryService.SearchCategoriesAsync(
                        auth.Connection.AccessToken,
                        auth.Settings.ClientId,
                        auth.Settings.ClientSecret,
                        term);

                    match =
                        results.FirstOrDefault(c => string.Equals(c.Name, term, StringComparison.OrdinalIgnoreCase))
                        ?? results.FirstOrDefault(c => c.ParentId is null &&
                            c.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

                    if (match is not null)
                    {
                        break;
                    }
                }

                if (match is not null && _categoryNameToId.TryAdd(match.Name, match.Id))
                {
                    _categoryComboBox.Items.Add(match.Name);

                    if (name == "Game")
                    {
                        _gameDirectoryId = match.Id;
                    }
                }
            }

            // Data-driven last resort for Game specifically: if nothing above found it,
            // this doesn't try to guess its name at all - it fetches a broad page of
            // categories and finds whichever parentId is referenced by the most children.
            // Since Game is the one directory described as having a much longer list of
            // individual titles than any other, its id should stand out clearly as the
            // most-referenced parent, regardless of what the directory itself is named or
            // whether a search for its name happens to surface it.
            if (_gameDirectoryId is null)
            {
                try
                {
                    IReadOnlyList<BlazeCategoryModel> broadSample = await _blazeCategoryService.GetCategoriesAsync(
                        auth.Connection.AccessToken,
                        auth.Settings.ClientId,
                        auth.Settings.ClientSecret);

                    var mostCommonParentId = broadSample
                        .Where(c => c.ParentId is not null)
                        .GroupBy(c => c.ParentId!.Value)
                        .OrderByDescending(group => group.Count())
                        .Select(group => (Id: group.Key, Count: group.Count()))
                        .FirstOrDefault();

                    if (mostCommonParentId.Count > 0)
                    {
                        // The directory's own row may or may not be in this same sample -
                        // use its real name if it happens to be there, otherwise fall back
                        // to a friendly label attached to the id that was actually found.
                        BlazeCategoryModel? directoryRow = broadSample.FirstOrDefault(c => c.Id == mostCommonParentId.Id);
                        string directoryName = directoryRow?.Name ?? "Game";

                        WadevoLogger.Info(
                            $"Blaze Game directory found via most-referenced-parent fallback: " +
                            $"id={mostCommonParentId.Id}, name=\"{directoryName}\", {mostCommonParentId.Count} children seen.");

                        if (_categoryNameToId.TryAdd(directoryName, mostCommonParentId.Id))
                        {
                            _categoryComboBox.Items.Add(directoryName);
                        }

                        _gameDirectoryId = mostCommonParentId.Id;
                    }
                }
                catch (Exception ex)
                {
                    WadevoLogger.Warning($"Game directory fallback lookup failed: {ex.Message}");
                }
            }

            if (IsDisposed)
            {
                return;
            }

            _categoriesLoaded = true;

            if (_categoryComboBox.Items.Count > 0)
            {
                _categoryComboBox.SelectedIndex = 0;
            }

            // Diagnostic counter removed - it did its job tracking down why "Game"
            // specifically wasn't being found; that's fixed and confirmed working now.

            WadevoLogger.Info(
                $"Blaze directories found: {string.Join(", ", _categoryComboBox.Items.Cast<string>())}");
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Blaze category list failed to load: {ex.Message}");
        }
    }

    private void ApplyDirectorySelection(Label gameSearchLabel)
    {
        // Only the Game directory drills into a further search, matching Blaze's own
        // picker - other directories (Music, IRL, Art, ...) are used directly.
        // Checked by id (found via search or the data-driven fallback) rather than by
        // whether the displayed name contains "Game" - the fallback's name isn't
        // guaranteed to literally contain that substring, so a text check could still
        // silently fail even after the directory itself was correctly found.
        bool isGameDirectory =
            _gameDirectoryId is not null &&
            _categoryNameToId.TryGetValue(_categoryComboBox.Text.Trim(), out int selectedId) &&
            selectedId == _gameDirectoryId;

        gameSearchLabel.Visible = isGameDirectory;
        _gameSearchTextBox.Visible = isGameDirectory;
        _gameSearchTextBox.Text = "";
        _gameResultsComboBox.Visible = false;
        _gameResultsComboBox.Items.Clear();
        _subCategoryNameToId.Clear();
    }

    private async Task SearchGamesAsync()
    {
        string term = _gameSearchTextBox.Text.Trim();

        if (term.Length < 2)
        {
            return;
        }

        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        if (!auth.IsAuthenticated)
        {
            return;
        }

        try
        {
            IReadOnlyList<BlazeCategoryModel> results = await _blazeCategoryService.SearchCategoriesAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Settings.ClientSecret,
                term);

            if (IsDisposed)
            {
                return;
            }

            // The API searches the whole catalog by name, not scoped to a single parent -
            // this narrows results down to actual games (children of something) rather
            // than potentially matching a directory or unrelated category by name too.
            _subCategoryNameToId.Clear();
            _gameResultsComboBox.Items.Clear();

            foreach (BlazeCategoryModel result in results.Where(r => r.ParentId is not null))
            {
                if (_subCategoryNameToId.TryAdd(result.Name, result.Id))
                {
                    _gameResultsComboBox.Items.Add(result.Name);
                }
            }

            _gameResultsComboBox.Visible = _gameResultsComboBox.Items.Count > 0;

            if (_gameResultsComboBox.Items.Count > 0)
            {
                _gameResultsComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Blaze game search failed: {ex.Message}");
        }
    }

    private async Task UpdateStreamInfoAsync()
    {
        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        if (!auth.IsAuthenticated)
        {
            _streamInfoStatusLabel.Text = "Connect Blaze first (Connections tab).";
            _streamInfoStatusLabel.ForeColor = WadevoTheme.Colors.Error;
            return;
        }

        string title = _streamTitleTextBox.Text.Trim();
        string lang = string.IsNullOrWhiteSpace(_languageTextBox.Text) ? "en" : _languageTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            _streamInfoStatusLabel.Text = "Title can't be empty.";
            _streamInfoStatusLabel.ForeColor = WadevoTheme.Colors.Error;
            return;
        }

        // Mirrors Blaze's own picker: if a specific game is chosen (the sub-picker is only
        // visible when the selected directory actually has one), that's the real category.
        // Otherwise the directory itself (Music, IRL, Art, ...) is the category directly.
        int? categoryId = _gameResultsComboBox.Visible
            ? (_subCategoryNameToId.TryGetValue(_gameResultsComboBox.Text.Trim(), out int subId) ? subId : null)
            : (_categoryNameToId.TryGetValue(_categoryComboBox.Text.Trim(), out int id) ? id : null);

        _updateStreamInfoButton.Enabled = false;
        _streamInfoStatusLabel.Text = "Updating…";
        _streamInfoStatusLabel.ForeColor = WadevoTheme.Colors.Warning;

        try
        {
            await _blazeChannelService.UpdateChannelInfoAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Settings.ClientSecret,
                title,
                lang,
                categoryId);

            if (IsDisposed)
            {
                return;
            }

            _streamInfoStatusLabel.Text = "✓ Stream info updated.";
            _streamInfoStatusLabel.ForeColor = WadevoTheme.Colors.Success;
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Blaze update channel info failed", ex);

            if (!IsDisposed)
            {
                _streamInfoStatusLabel.Text = $"Couldn't update: {ex.Message.Split("\r\n")[0]}";
                _streamInfoStatusLabel.ForeColor = WadevoTheme.Colors.Error;
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                _updateStreamInfoButton.Enabled = true;
            }
        }
    }

    private void SetBlazeStatus(string text, Color color)
    {
        if (IsDisposed)
        {
            return;
        }

        _blazeStatusBadge.Text = text;
        _blazeStatusBadge.ForeColor = color;
    }

    private void RefreshAll()
    {
        RefreshStatsGrid();
        RefreshActivity();
    }

    private void RefreshStatsGrid()
    {
        _statsGrid.SuspendLayout();

        foreach (Control control in _statsGrid.Controls.Cast<Control>().ToList())
        {
            _statsGrid.Controls.Remove(control);
            control.Dispose();
        }

        DashboardStatsModel stats = _statsService.GetStats();

        (string Label, int Value, Color Color)[] tiles =
        {
            ("Followers", stats.FollowCount, WadevoTheme.Colors.Accent),
            ("Subscriptions", stats.SubscribeCount, WadevoTheme.Colors.Success),
            ("Gift Subs", stats.GiftSubCount, WadevoTheme.Colors.Warning),
            ("Raids", stats.RaidCount, WadevoTheme.Colors.Error),
            ("Votes", stats.VoteCount, WadevoTheme.Colors.Cyan),
            ("VIPs", stats.VipCount, WadevoTheme.Colors.Purple),
            ("Commands Used", stats.CommandsExecutedCount, WadevoTheme.Colors.Accent),
            ("Song Requests", stats.SongRequestsCount, WadevoTheme.Colors.Cyan),
            ("Chat Messages", stats.ChatMessagesCount, WadevoTheme.Colors.TextMuted)
        };

        int tileWidth = 190;
        int tileHeight = 44;
        int gap = 10;
        int columns = 3;

        for (int i = 0; i < tiles.Length; i++)
        {
            int column = i % columns;
            int row = i / columns;

            Panel tile = new()
            {
                Location = new Point(column * (tileWidth + gap), row * (tileHeight + gap)),
                Size = new Size(tileWidth, tileHeight),
                BackColor = WadevoTheme.Colors.BackgroundSoft
            };

            // Number beside the label instead of stacked above it - cuts each tile's
            // height by nearly a third, which is what was pushing the grid (and the whole
            // card) taller than the Recent Activity panel next to it.
            Label valueLabel = new()
            {
                Text = tiles[i].Value.ToString(),
                Location = new Point(12, 0),
                Size = new Size(52, tileHeight),
                Font = WadevoTheme.Fonts.CardHeader,
                ForeColor = tiles[i].Color,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label nameLabel = new()
            {
                Text = tiles[i].Label,
                Location = new Point(68, 0),
                Size = new Size(tileWidth - 80, tileHeight),
                Font = WadevoTheme.Fonts.Small,
                ForeColor = WadevoTheme.Colors.TextMuted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            tile.Controls.Add(valueLabel);
            tile.Controls.Add(nameLabel);

            _statsGrid.Controls.Add(tile);
        }

        _statsGrid.ResumeLayout();
    }

    private void RefreshActivity()
    {
        _activityPanel.Content.SuspendLayout();

        foreach (Control control in _activityPanel.Content.Controls.Cast<Control>().ToList())
        {
            _activityPanel.Content.Controls.Remove(control);
            control.Dispose();
        }

        IReadOnlyList<string> activity = _statsService.GetRecentActivity();

        if (activity.Count == 0)
        {
            Label empty = new()
            {
                Text = "Nothing yet - activity will show up here as your stream runs.",
                Size = new Size(600, 30),
                Font = WadevoTheme.Fonts.Default,
                ForeColor = WadevoTheme.Colors.TextMuted,
                BackColor = Color.Transparent
            };

            _activityPanel.Content.Controls.Add(empty);
        }

        foreach (string entry in activity)
        {
            Label entryLabel = new()
            {
                Text = entry,
                Size = new Size(600, 24),
                Font = WadevoTheme.Fonts.Default,
                ForeColor = WadevoTheme.Colors.Text,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };

            _activityPanel.Content.Controls.Add(entryLabel);
        }

        _activityPanel.Content.ResumeLayout();
        _activityPanel.RefreshLayout();
    }
}
