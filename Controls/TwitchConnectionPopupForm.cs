namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Twitch;

public sealed class TwitchConnectionPopupForm : WadevoDialogForm
{
    private readonly TwitchAuthenticationService _authenticationService = TwitchAuthenticationService.Shared;
    private readonly TwitchLiveEventService _liveEventService = TwitchLiveEventService.Shared;

    private readonly Label _statusLabel = new();
    private readonly Label _detailsLabel = new();
    private readonly ListBox _eventListBox = new();

    private readonly WadevoButton _connectButton = new();
    private readonly WadevoButton _eventsToggleButton = new();
    private readonly WadevoButton _setupCredentialsButton = new();

    private readonly BlockedWordsStore _blockedWordsStore = new();
    private readonly TextBox _blockedWordsTextBox = new();

    private readonly WadevoAppSettingsStore _appSettingsStore = new();

    public TwitchConnectionPopupForm()
        : base("🟣 Twitch Connection")
    {
        Size = new Size(620, 700);
        ContentPanel.AutoScroll = true;

        _authenticationService.ConnectionStateChanged += (_, _) => SafeRefreshStatus();
        _liveEventService.StatusChanged += (_, _) => SafeRefreshStatus();
        _liveEventService.EventReceived += EventClient_EventReceived;
        _liveEventService.LogMessage += (_, message) => SafeAddEventLog(message);

        BuildConnectionSection();
        AddDivider(182);
        BuildEventFeedSection();
        AddDivider(578);
        BuildBlockedWordsSection();
        AddDivider(800);
        BuildBotIdentitySection();

        // Panel.AutoScroll's automatic range calculation isn't always reliable with a mix
        // of absolute-positioned children added across several separate methods like this -
        // computing it explicitly from the actual bottom-most control (with some padding)
        // guarantees the scrollable area really reaches the end of the last section.
        int contentBottom = ContentPanel.Controls.Cast<Control>().Max(control => control.Bottom);
        ContentPanel.AutoScrollMinSize = new Size(0, contentBottom + 60);

        RefreshStatus();
    }

    private void AddDivider(int y)
    {
        Panel divider = new()
        {
            Location = new Point(24, y),
            Size = new Size(560, 1),
            BackColor = Color.FromArgb(90, WadevoTheme.Colors.Accent)
        };

        ContentPanel.Controls.Add(divider);
    }

    private void BuildConnectionSection()
    {
        _statusLabel.Location = new Point(24, 16);
        _statusLabel.Size = new Size(500, 26);
        _statusLabel.Font = WadevoTheme.Fonts.Bold;
        _statusLabel.BackColor = Color.Transparent;

        _detailsLabel.Text = "Click Connect and log into your Twitch account.";
        _detailsLabel.Location = new Point(24, 44);
        _detailsLabel.Size = new Size(560, 34);
        _detailsLabel.Font = WadevoTheme.Fonts.Small;
        _detailsLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _detailsLabel.BackColor = Color.Transparent;

        _connectButton.ButtonText = "Connect";
        _connectButton.Location = new Point(24, 82);
        _connectButton.Size = new Size(120, 38);
        _connectButton.AccentColor = WadevoTheme.Colors.Purple;
        _connectButton.ButtonClicked += ConnectButton_Click;

        WadevoButton disconnectButton = new()
        {
            ButtonText = "Disconnect",
            Location = new Point(152, 82),
            Size = new Size(120, 38),
            AccentColor = WadevoTheme.Colors.Error
        };
        disconnectButton.ButtonClicked += DisconnectButton_Click;

        // Only shown when the placeholder credentials are still in place - once real ones
        // are baked into TwitchAppCredentials.cs for a distributed build, this button (and
        // the whole Client ID/Secret concept) becomes irrelevant to an end user, and
        // leaving it visible only invites confusion or accidental exposure (someone
        // streaming or screenshotting the app could reveal their Client ID on screen).
        bool credentialsAreStillPlaceholder =
            TwitchAppCredentials.ClientId == "PASTE_YOUR_TWITCH_CLIENT_ID_HERE";

        _setupCredentialsButton.ButtonText = "Twitch App Setup";
        _setupCredentialsButton.Location = new Point(24, 128);
        _setupCredentialsButton.Size = new Size(150, 38);
        _setupCredentialsButton.AccentColor = WadevoTheme.Colors.Cyan;
        _setupCredentialsButton.Visible = credentialsAreStillPlaceholder;

        _setupCredentialsButton.ButtonClicked += (_, _) =>
        {
            using TwitchDevCredentialsForm form = new();

            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            (string ClientId, string ClientSecret)? saved = new TwitchDevCredentialsStore().Load();

            if (saved is not null)
            {
                _authenticationService.Settings.ClientId = saved.Value.ClientId;
                _authenticationService.Settings.ClientSecret = saved.Value.ClientSecret;
                _setupCredentialsButton.Visible = false;
            }

            RefreshStatus();
        };

        _eventsToggleButton.ButtonText = "Start Events";
        _eventsToggleButton.Location = new Point(184, 128);
        _eventsToggleButton.Size = new Size(140, 38);
        _eventsToggleButton.AccentColor = WadevoTheme.Colors.Cyan;
        _eventsToggleButton.ButtonClicked += EventsToggleButton_Click;

        ContentPanel.Controls.Add(_statusLabel);
        ContentPanel.Controls.Add(_detailsLabel);
        ContentPanel.Controls.Add(_connectButton);
        ContentPanel.Controls.Add(disconnectButton);
        ContentPanel.Controls.Add(_setupCredentialsButton);
        ContentPanel.Controls.Add(_eventsToggleButton);
    }

    private void BuildEventFeedSection()
    {
        Label header = new()
        {
            Text = "Live Event Feed",
            Location = new Point(24, 198),
            Size = new Size(300, 24),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        _eventListBox.Location = new Point(24, 228);
        _eventListBox.Size = new Size(560, 340);
        _eventListBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _eventListBox.ForeColor = WadevoTheme.Colors.Text;
        _eventListBox.BorderStyle = BorderStyle.FixedSingle;
        _eventListBox.Font = WadevoTheme.Fonts.Small;

        ContentPanel.Controls.Add(header);
        ContentPanel.Controls.Add(_eventListBox);
    }

    // This reuses the exact same BlockedWordsStore as the Blaze popup - blocked words
    // aren't platform-specific, so editing the list here (or there) updates the one
    // shared list both platforms check against.
    private void BuildBlockedWordsSection()
    {
        BlockedWordsSettings settings = _blockedWordsStore.Load();

        Label header = new()
        {
            Text = "🚫 Blocked Words",
            Location = new Point(24, 594),
            Size = new Size(300, 26),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Purple,
            BackColor = Color.Transparent
        };

        Label sharedNote = new()
        {
            Text = "Shared with Blaze - one list, checked on every platform you connect.",
            Location = new Point(24, 620),
            Size = new Size(560, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoCheckBox enabledCheckBox = new()
        {
            Text = "Automatically delete messages containing a blocked word",
            Location = new Point(24, 646),
            Size = new Size(560, 24),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = settings.IsEnabled
        };

        WadevoCheckBox timeoutCheckBox = new()
        {
            Text = "Also time out that viewer for 10 minutes",
            Location = new Point(24, 672),
            Size = new Size(560, 24),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = settings.MuteOnBlock
        };

        Label listLabel = new()
        {
            Text = "One word or phrase per line:",
            Location = new Point(24, 702),
            Size = new Size(300, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _blockedWordsTextBox.Multiline = true;
        _blockedWordsTextBox.Location = new Point(24, 726);
        _blockedWordsTextBox.Size = new Size(560, 60);
        _blockedWordsTextBox.Font = WadevoTheme.Fonts.Default;
        _blockedWordsTextBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _blockedWordsTextBox.ForeColor = WadevoTheme.Colors.Text;
        _blockedWordsTextBox.BorderStyle = BorderStyle.FixedSingle;
        _blockedWordsTextBox.ScrollBars = ScrollBars.Vertical;
        _blockedWordsTextBox.Text = string.Join(Environment.NewLine, settings.Words);

        void SaveBlockedWordsSettings()
        {
            BlockedWordsSettings current = _blockedWordsStore.Load();

            current.IsEnabled = enabledCheckBox.Checked;
            current.MuteOnBlock = timeoutCheckBox.Checked;
            current.Words = _blockedWordsTextBox.Text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(word => word.Length > 0)
                .ToList();

            _blockedWordsStore.Save(current);
        }

        enabledCheckBox.CheckedChanged += (_, _) => SaveBlockedWordsSettings();
        timeoutCheckBox.CheckedChanged += (_, _) => SaveBlockedWordsSettings();
        _blockedWordsTextBox.Leave += (_, _) => SaveBlockedWordsSettings();

        ContentPanel.Controls.Add(header);
        ContentPanel.Controls.Add(sharedNote);
        ContentPanel.Controls.Add(enabledCheckBox);
        ContentPanel.Controls.Add(timeoutCheckBox);
        ContentPanel.Controls.Add(listLabel);
        ContentPanel.Controls.Add(_blockedWordsTextBox);
    }

    private void BuildBotIdentitySection()
    {
        TwitchBotAuthenticationService botAuth = TwitchBotAuthenticationService.Shared;
        WadevoAppSettingsModel settings = _appSettingsStore.Load();

        Label header = new()
        {
            Text = "🤖 Bot Identity",
            Location = new Point(24, 816),
            Size = new Size(300, 26),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Purple,
            BackColor = Color.Transparent
        };

        Label description = new()
        {
            Text = "By default, commands post to chat using your own Twitch name. Connect a separate " +
                   "Twitch account here to give Wadevo its own identity instead - a real bot with its " +
                   "own name and avatar, not you. Note: the bot account usually needs to be a " +
                   "moderator on your channel for Twitch to allow it to send messages.",
            Location = new Point(24, 844),
            Size = new Size(560, 50),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label statusLabel = new()
        {
            Location = new Point(24, 900),
            Size = new Size(400, 24),
            Font = WadevoTheme.Fonts.Bold,
            BackColor = Color.Transparent
        };

        void RefreshStatus()
        {
            if (botAuth.IsConnected)
            {
                statusLabel.Text = $"🟢 Bot connected as {botAuth.BotUsername}";
                statusLabel.ForeColor = WadevoTheme.Colors.Success;
            }
            else
            {
                statusLabel.Text = "⚪ No bot account connected";
                statusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
            }
        }

        RefreshStatus();

        WadevoButton connectBotButton = new()
        {
            ButtonText = "Connect Bot Account",
            Location = new Point(24, 930),
            Size = new Size(190, 36),
            AccentColor = WadevoTheme.Colors.Purple
        };

        WadevoButton disconnectBotButton = new()
        {
            ButtonText = "Disconnect Bot",
            Location = new Point(222, 930),
            Size = new Size(150, 36),
            AccentColor = WadevoTheme.Colors.Error
        };

        WadevoCheckBox useBotToggle = new()
        {
            Text = "Commands post as the bot (instead of me)",
            Location = new Point(24, 976),
            Size = new Size(400, 26),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = settings.UseTwitchBotIdentityForCommands
        };

        connectBotButton.ButtonClicked += async (_, _) =>
        {
            try
            {
                connectBotButton.Enabled = false;

                Task<bool> loginTask = botAuth.CompleteLoginAsync();

                botAuth.BeginLogin();

                statusLabel.Text = "Log in with the BOT's Twitch account, not your own, then approve access.";
                statusLabel.ForeColor = WadevoTheme.Colors.Warning;

                bool success = await loginTask;

                if (!success)
                {
                    statusLabel.Text = botAuth.Connection.StatusMessage;
                    statusLabel.ForeColor = WadevoTheme.Colors.Error;
                    return;
                }

                RefreshStatus();
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
                statusLabel.ForeColor = WadevoTheme.Colors.Error;
            }
            finally
            {
                connectBotButton.Enabled = true;
            }
        };

        disconnectBotButton.ButtonClicked += (_, _) =>
        {
            botAuth.Reset();
            RefreshStatus();
        };

        useBotToggle.CheckedChanged += (_, _) =>
        {
            WadevoAppSettingsModel current = _appSettingsStore.Load();
            current.UseTwitchBotIdentityForCommands = useBotToggle.Checked;
            _appSettingsStore.Save(current);
        };

        ContentPanel.Controls.Add(header);
        ContentPanel.Controls.Add(description);
        ContentPanel.Controls.Add(statusLabel);
        ContentPanel.Controls.Add(connectBotButton);
        ContentPanel.Controls.Add(disconnectBotButton);
        ContentPanel.Controls.Add(useBotToggle);
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        if (!_authenticationService.Settings.IsConfigured)
        {
            _detailsLabel.Text = "Wadevo's Twitch app credentials haven't been set up yet. " +
                                  "Register an app at dev.twitch.tv/console/apps and add the Client ID/Secret.";
            _detailsLabel.ForeColor = WadevoTheme.Colors.Error;
            return;
        }

        try
        {
            _connectButton.Enabled = false;

            Task<bool> loginTask = _authenticationService.CompleteLoginAsync();

            _authenticationService.BeginLogin();

            _detailsLabel.Text = "Twitch login opened in your browser. Waiting for approval...";
            _detailsLabel.ForeColor = WadevoTheme.Colors.Warning;

            bool success = await loginTask;

            if (!success)
            {
                _detailsLabel.Text = _authenticationService.Connection.StatusMessage;
                _detailsLabel.ForeColor = WadevoTheme.Colors.Error;
                return;
            }

            _detailsLabel.Text = $"Connected to Twitch as {_authenticationService.Connection.Username}.";
            _detailsLabel.ForeColor = WadevoTheme.Colors.Success;
        }
        catch (Exception ex)
        {
            _detailsLabel.Text = ex.Message;
            _detailsLabel.ForeColor = WadevoTheme.Colors.Error;
        }
        finally
        {
            _connectButton.Enabled = true;
            RefreshStatus();
        }
    }

    private void DisconnectButton_Click(object? sender, EventArgs e)
    {
        _ = _liveEventService.StopAsync();
        _authenticationService.Reset();
        _detailsLabel.Text = "Click Connect and log into your Twitch account.";
        _detailsLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        RefreshStatus();
    }

    private async void EventsToggleButton_Click(object? sender, EventArgs e)
    {
        if (_liveEventService.IsListening)
        {
            await _liveEventService.StopAsync();
        }
        else
        {
            if (!_authenticationService.IsAuthenticated)
            {
                _detailsLabel.Text = "Connect to Twitch before starting events.";
                _detailsLabel.ForeColor = WadevoTheme.Colors.Error;
                return;
            }

            try
            {
                await _liveEventService.StartAsync();
            }
            catch (Exception ex)
            {
                _detailsLabel.Text = ex.Message;
                _detailsLabel.ForeColor = WadevoTheme.Colors.Error;
            }
        }

        RefreshStatus();
    }

    private void EventClient_EventReceived(object? sender, TwitchEvent twitchEvent)
    {
        string line = twitchEvent.EventType switch
        {
            TwitchEventType.ChatMessage => $"💬 {twitchEvent.Username}: {twitchEvent.Message}",
            TwitchEventType.Follow => $"⭐ {twitchEvent.Username} followed",
            TwitchEventType.Subscribe => $"🎉 {twitchEvent.Username} subscribed",
            TwitchEventType.GiftSub => $"🎁 {twitchEvent.Username} gifted a sub",
            TwitchEventType.Cheer => $"💎 {twitchEvent.Username} cheered {twitchEvent.BitsCheered} bits",
            TwitchEventType.Raid => $"🚀 {twitchEvent.Username} raided with {twitchEvent.ViewerCount} viewers",
            TwitchEventType.StreamOnline => "🟢 Stream went live",
            TwitchEventType.StreamOffline => "⚫ Stream went offline",
            TwitchEventType.Connected => "🟣 Connected to Twitch event stream",
            TwitchEventType.Disconnected => "⚪ Disconnected from Twitch event stream",
            TwitchEventType.Error => $"⚠️ {twitchEvent.Message}",
            _ => twitchEvent.Message ?? "Unknown event"
        };

        SafeAddEventLog(line);
    }

    private void SafeAddEventLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new MethodInvoker(() => SafeAddEventLog(message)));
            return;
        }

        _eventListBox.Items.Insert(0, message);

        while (_eventListBox.Items.Count > 100)
            _eventListBox.Items.RemoveAt(_eventListBox.Items.Count - 1);
    }

    private void SafeRefreshStatus()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new MethodInvoker(RefreshStatus));
            return;
        }

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        _eventsToggleButton.ButtonText = _liveEventService.IsListening ? "Stop Events" : "Start Events";

        if (_liveEventService.IsListening)
        {
            _statusLabel.Text = "🟣 Twitch Event Stream Connected";
            _statusLabel.ForeColor = WadevoTheme.Colors.Purple;
            return;
        }

        if (!_authenticationService.IsConfigured)
        {
            _statusLabel.Text = "⚪ Not Configured";
            _statusLabel.ForeColor = WadevoTheme.Colors.Warning;
            return;
        }

        if (_authenticationService.IsAuthenticated)
        {
            _statusLabel.Text = "🟢 Connected";
            _statusLabel.ForeColor = WadevoTheme.Colors.Success;
            return;
        }

        _statusLabel.Text = "⚪ Ready to Connect";
        _statusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
    }
}
