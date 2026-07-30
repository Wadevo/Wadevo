namespace Wadevo.Controls;

using System.Diagnostics;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Blaze;

public sealed class BlazeConnectionPopupForm : WadevoDialogForm
{
    private readonly BlazeAuthenticationService _authenticationService = BlazeAuthenticationService.Shared;
    private readonly BlazeLiveEventService _liveEventService = BlazeLiveEventService.Shared;
    private readonly WadevoAppSettingsStore _appSettingsStore = new();

    private readonly Label _statusLabel = new();
    private readonly Label _detailsLabel = new();
    private readonly ListBox _eventListBox = new();

    private readonly WadevoButton _connectButton = new();
    private readonly WadevoButton _eventsToggleButton = new();

    public BlazeConnectionPopupForm()
        : base("🔥 Blaze Connection")
    {
        Size = new Size(620, 820);
        ContentPanel.AutoScroll = true;

        _authenticationService.ConnectionStateChanged += (_, _) => RefreshStatus();
        _liveEventService.StatusChanged += (_, _) => RefreshStatus();
        _liveEventService.EventReceived += LiveEventService_EventReceived;
        _liveEventService.LogMessage += (_, message) => SafeAddEventLog(message);

        BuildConnectionSection();
        AddDivider(126);
        BuildFeatureToggleSection();
        AddDivider(222);
        BuildBotAccountSection();
        AddDivider(418);
        BuildBlockedWordsSection();
        AddDivider(676);
        BuildEventFeedSection();

        BlazeDemoCommandSeeder.EnsureDemoCommands();
        RefreshStatus();
    }

    // A thin divider between sections (Connection, Bot Identity, Blocked Words, Connection
    // Log) - without one, the popup reads as one long unbroken wall of controls, which is
    // what made it feel confusing/intimidating at a glance despite each section being
    // simple on its own.
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

        _detailsLabel.Text = "Click Connect and log into your Blaze account.";
        _detailsLabel.Location = new Point(24, 44);
        _detailsLabel.Size = new Size(560, 34);
        _detailsLabel.Font = WadevoTheme.Fonts.Small;
        _detailsLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _detailsLabel.BackColor = Color.Transparent;

        _connectButton.ButtonText = "Connect";
        _connectButton.Location = new Point(24, 82);
        _connectButton.Size = new Size(120, 38);
        _connectButton.AccentColor = WadevoTheme.Colors.Accent;
        _connectButton.ButtonClicked += ConnectButton_Click;

        WadevoButton resetButton = new()
        {
            ButtonText = "Disconnect",
            Location = new Point(152, 82),
            Size = new Size(120, 38),
            AccentColor = WadevoTheme.Colors.Error
        };

        resetButton.ButtonClicked += (_, _) =>
        {
            _authenticationService.Reset();
            RefreshStatus();
        };

        // Only shown when the placeholder credentials are still in place - once real ones
        // are baked into BlazeAppCredentials.cs for a distributed build, this button (and
        // the whole Client ID/Secret concept) becomes irrelevant to an end user, and
        // leaving it visible only invites confusion or accidental exposure (someone
        // streaming or screenshotting the app could reveal their Client ID on screen).
        bool credentialsAreStillPlaceholder =
            BlazeAppCredentials.ClientId == "PASTE_YOUR_BLAZE_CLIENT_ID_HERE";

        WadevoButton setupCredentialsButton = new()
        {
            ButtonText = "Blaze App Setup",
            Location = new Point(280, 82),
            Size = new Size(150, 38),
            AccentColor = WadevoTheme.Colors.Cyan,
            Visible = credentialsAreStillPlaceholder
        };

        setupCredentialsButton.ButtonClicked += (_, _) =>
        {
            using BlazeDevCredentialsForm form = new();

            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            (string ClientId, string ClientSecret)? saved = new BlazeDevCredentialsStore().Load();

            if (saved is not null)
            {
                _authenticationService.Settings.ClientId = saved.Value.ClientId;
                _authenticationService.Settings.ClientSecret = saved.Value.ClientSecret;
            }

            RefreshStatus();
        };

        _eventsToggleButton.ButtonText = "Start Events";
        // Fills the gap left behind when the setup button above is hidden for a
        // distributed build (real credentials already baked in), so the row still looks
        // intentional either way rather than leaving an empty gap.
        _eventsToggleButton.Location = credentialsAreStillPlaceholder
            ? new Point(440, 82)
            : new Point(280, 82);
        _eventsToggleButton.Size = new Size(140, 38);
        _eventsToggleButton.AccentColor = WadevoTheme.Colors.Purple;
        _eventsToggleButton.ButtonClicked += EventsToggleButton_Click;

        ContentPanel.Controls.Add(_statusLabel);
        ContentPanel.Controls.Add(_detailsLabel);
        ContentPanel.Controls.Add(_connectButton);
        ContentPanel.Controls.Add(resetButton);
        ContentPanel.Controls.Add(setupCredentialsButton);
        ContentPanel.Controls.Add(_eventsToggleButton);
    }

    private void BuildFeatureToggleSection()
    {
        Label sectionHeader = new()
        {
            Text = "Features on this platform",
            Location = new Point(24, 136),
            Size = new Size(400, 24),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        WadevoAppSettingsModel settings = _appSettingsStore.Load();

        WadevoCheckBox commandsToggle = new()
        {
            Text = "Commands post to Blaze chat",
            Location = new Point(24, 166),
            Size = new Size(400, 26),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = settings.BlazeCommandsEnabled
        };

        Label toggleNote = new()
        {
            Text = "On-screen alerts always work regardless of this setting - they're OBS overlays, not chat messages.",
            Location = new Point(24, 194),
            Size = new Size(560, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        commandsToggle.CheckedChanged += (_, _) =>
        {
            WadevoAppSettingsModel current = _appSettingsStore.Load();
            current.BlazeCommandsEnabled = commandsToggle.Checked;
            _appSettingsStore.Save(current);
        };

        ContentPanel.Controls.Add(sectionHeader);
        ContentPanel.Controls.Add(commandsToggle);
        ContentPanel.Controls.Add(toggleNote);
    }

    private void BuildBotAccountSection()
    {
        BlazeBotAuthenticationService botAuth = BlazeBotAuthenticationService.Shared;
        WadevoAppSettingsModel settings = _appSettingsStore.Load();

        Label header = new()
        {
            Text = "🤖 Bot Identity",
            Location = new Point(24, 232),
            Size = new Size(300, 26),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Purple,
            BackColor = Color.Transparent
        };

        Label description = new()
        {
            Text = "By default, commands post to chat using your own Blaze name. Connect a separate " +
                   "Blaze account here to give Wadevo its own identity instead - a real bot with its " +
                   "own name and avatar, not you.",
            Location = new Point(24, 260),
            Size = new Size(560, 40),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label statusLabel = new()
        {
            Location = new Point(24, 306),
            Size = new Size(400, 24),
            Font = WadevoTheme.Fonts.Bold,
            BackColor = Color.Transparent
        };

        void RefreshStatus()
        {
            if (botAuth.IsConnected)
            {
                statusLabel.Text = "🟢 Bot account connected";
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
            Location = new Point(24, 336),
            Size = new Size(190, 36),
            AccentColor = WadevoTheme.Colors.Purple
        };

        WadevoButton disconnectBotButton = new()
        {
            ButtonText = "Disconnect Bot",
            Location = new Point(222, 336),
            Size = new Size(150, 36),
            AccentColor = WadevoTheme.Colors.Error
        };

        WadevoCheckBox useBotToggle = new()
        {
            Text = "Commands post as the bot (instead of me)",
            Location = new Point(24, 382),
            Size = new Size(400, 26),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = settings.UseBotIdentityForCommands
        };

        connectBotButton.ButtonClicked += async (_, _) =>
        {
            try
            {
                connectBotButton.Enabled = false;
                statusLabel.Text = "Opening Blaze login for the bot account...";
                statusLabel.ForeColor = WadevoTheme.Colors.Warning;

                BlazeAuthorizationUrlResponse authorization = await botAuth.GenerateAuthorizationUrlAsync();

                Task<BlazeAuthenticationResult> loginTask = botAuth.CompleteLoginAsync();

                Process.Start(new ProcessStartInfo
                {
                    FileName = authorization.Url,
                    UseShellExecute = true
                });

                statusLabel.Text = "Log in with the BOT's Blaze account, not your own, then approve access.";
                statusLabel.ForeColor = WadevoTheme.Colors.Warning;

                BlazeAuthenticationResult result = await loginTask;

                if (!result.Success)
                {
                    statusLabel.Text = result.Message;
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
            current.UseBotIdentityForCommands = useBotToggle.Checked;
            _appSettingsStore.Save(current);
        };

        ContentPanel.Controls.Add(header);
        ContentPanel.Controls.Add(description);
        ContentPanel.Controls.Add(statusLabel);
        ContentPanel.Controls.Add(connectBotButton);
        ContentPanel.Controls.Add(disconnectBotButton);
        ContentPanel.Controls.Add(useBotToggle);
    }

    private readonly BlockedWordsStore _blockedWordsStore = new();
    private readonly TextBox _blockedWordsTextBox = new();

    private void BuildBlockedWordsSection()
    {
        BlockedWordsSettings settings = _blockedWordsStore.Load();

        Label header = new()
        {
            Text = "🚫 Blocked Words",
            Location = new Point(24, 432),
            Size = new Size(300, 26),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Purple,
            BackColor = Color.Transparent
        };

        WadevoCheckBox enabledCheckBox = new()
        {
            Text = "Automatically delete messages containing a blocked word",
            Location = new Point(24, 462),
            Size = new Size(560, 24),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = settings.IsEnabled
        };

        WadevoCheckBox muteCheckBox = new()
        {
            Text = "Also mute that viewer for 10 minutes",
            Location = new Point(24, 488),
            Size = new Size(560, 24),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = settings.MuteOnBlock
        };

        Label listLabel = new()
        {
            Text = "One word or phrase per line:",
            Location = new Point(24, 518),
            Size = new Size(300, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _blockedWordsTextBox.Multiline = true;
        _blockedWordsTextBox.Location = new Point(24, 542);
        _blockedWordsTextBox.Size = new Size(560, 60);
        _blockedWordsTextBox.Font = WadevoTheme.Fonts.Default;
        _blockedWordsTextBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _blockedWordsTextBox.ForeColor = WadevoTheme.Colors.Text;
        _blockedWordsTextBox.BorderStyle = BorderStyle.FixedSingle;
        _blockedWordsTextBox.ScrollBars = ScrollBars.Vertical;
        _blockedWordsTextBox.Text = string.Join(Environment.NewLine, settings.Words);

        WadevoButton importButton = new()
        {
            ButtonText = "📤 Import List...",
            Location = new Point(24, 610),
            Size = new Size(170, 34),
            AccentColor = WadevoTheme.Colors.Cyan
        };

        WadevoButton exportButton = new()
        {
            ButtonText = "📥 Export List...",
            Location = new Point(204, 610),
            Size = new Size(170, 34),
            AccentColor = WadevoTheme.Colors.TextMuted
        };

        Label importExportStatus = new()
        {
            Location = new Point(24, 650),
            Size = new Size(560, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Success,
            BackColor = Color.Transparent
        };

        importButton.ButtonClicked += (_, _) =>
        {
            using OpenFileDialog dialog = new()
            {
                Title = "Import Blocked Words List",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                string[] importedWords = File.ReadAllLines(dialog.FileName);

                List<string> existingWords = _blockedWordsTextBox.Text
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(w => w.Length > 0)
                    .ToList();

                // Merge rather than replace - a shared list is meant to add to what's
                // already there, not silently wipe out words someone already set up.
                HashSet<string> merged = new(existingWords, StringComparer.OrdinalIgnoreCase);
                int addedCount = 0;

                foreach (string word in importedWords)
                {
                    string trimmed = word.Trim();

                    if (trimmed.Length > 0 && merged.Add(trimmed))
                    {
                        addedCount++;
                    }
                }

                _blockedWordsTextBox.Text = string.Join(Environment.NewLine, merged.OrderBy(w => w, StringComparer.OrdinalIgnoreCase));

                importExportStatus.ForeColor = WadevoTheme.Colors.Success;
                importExportStatus.Text = $"✅ Added {addedCount} new word(s) from the file.";

                SaveBlockedWordsSettings();
            }
            catch (Exception ex)
            {
                importExportStatus.ForeColor = WadevoTheme.Colors.Error;
                importExportStatus.Text = $"❌ Couldn't read that file: {ex.Message}";
            }
        };

        exportButton.ButtonClicked += (_, _) =>
        {
            using SaveFileDialog dialog = new()
            {
                Title = "Export Blocked Words List",
                Filter = "Text files (*.txt)|*.txt",
                FileName = "blocked-words.txt"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                File.WriteAllText(dialog.FileName, _blockedWordsTextBox.Text);

                importExportStatus.ForeColor = WadevoTheme.Colors.Success;
                importExportStatus.Text = "✅ Saved. Share that file with other streamers if you'd like.";
            }
            catch (Exception ex)
            {
                importExportStatus.ForeColor = WadevoTheme.Colors.Error;
                importExportStatus.Text = $"❌ Couldn't save that file: {ex.Message}";
            }
        };

        void SaveBlockedWordsSettings()
        {
            BlockedWordsSettings current = _blockedWordsStore.Load();

            current.IsEnabled = enabledCheckBox.Checked;
            current.MuteOnBlock = muteCheckBox.Checked;
            current.Words = _blockedWordsTextBox.Text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(word => word.Length > 0)
                .ToList();

            _blockedWordsStore.Save(current);
        }

        enabledCheckBox.CheckedChanged += (_, _) => SaveBlockedWordsSettings();
        muteCheckBox.CheckedChanged += (_, _) => SaveBlockedWordsSettings();
        _blockedWordsTextBox.Leave += (_, _) => SaveBlockedWordsSettings();

        ContentPanel.Controls.Add(header);
        ContentPanel.Controls.Add(enabledCheckBox);
        ContentPanel.Controls.Add(muteCheckBox);
        ContentPanel.Controls.Add(listLabel);
        ContentPanel.Controls.Add(_blockedWordsTextBox);
        ContentPanel.Controls.Add(importButton);
        ContentPanel.Controls.Add(exportButton);
        ContentPanel.Controls.Add(importExportStatus);
    }

    private void BuildEventFeedSection()
    {
        Label header = new()
        {
            Text = "⚡ Connection Log",
            Location = new Point(24, 682),
            Size = new Size(300, 26),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Purple,
            BackColor = Color.Transparent
        };

        Label subtitle = new()
        {
            Text = "Raw connection/event messages for troubleshooting - for a readable feed of follows, subs, and votes, see the Dashboard's Recent Activity instead.",
            Location = new Point(24, 706),
            Size = new Size(560, 18),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _eventListBox.Location = new Point(24, 728);
        _eventListBox.Size = new Size(560, 194);
        _eventListBox.Font = WadevoTheme.Fonts.Small;
        _eventListBox.ForeColor = WadevoTheme.Colors.Text;
        _eventListBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _eventListBox.BorderStyle = BorderStyle.FixedSingle;

        ContentPanel.Controls.Add(header);
        ContentPanel.Controls.Add(subtitle);
        ContentPanel.Controls.Add(_eventListBox);
    }

    private async void EventsToggleButton_Click(object? sender, EventArgs e)
    {
        if (_liveEventService.IsListening)
        {
            await _liveEventService.StopAsync();
        }
        else
        {
            _eventsToggleButton.Enabled = false;
            AddEventLog("Starting Blaze event stream...");

            await _liveEventService.StartAsync();

            _eventsToggleButton.Enabled = true;
        }

        RefreshStatus();
    }

    private void SafeAddEventLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new MethodInvoker(() => SafeAddEventLog(message)));
            return;
        }

        AddEventLog(message);
    }

    private void LiveEventService_EventReceived(object? sender, BlazeEvent blazeEvent)
    {
        SafeAddEventLog(BlazeEventMessageFormatter.Format(blazeEvent));
    }

    private void AddEventLog(string message)
    {
        BlazeEventLogEntry entry = new()
        {
            Message = message
        };

        _eventListBox.Items.Insert(0, entry.DisplayText);

        while (_eventListBox.Items.Count > 100)
            _eventListBox.Items.RemoveAt(_eventListBox.Items.Count - 1);
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        if (!_authenticationService.Settings.IsConfigured)
        {
            _detailsLabel.Text = "Wadevo's Blaze app credentials haven't been set up yet. " +
                                  "Click \"Blaze App Setup\" (one-time only).";
            _detailsLabel.ForeColor = WadevoTheme.Colors.Error;
            return;
        }

        try
        {
            _connectButton.Enabled = false;
            _detailsLabel.Text = "Generating Blaze authorization URL...";
            _detailsLabel.ForeColor = WadevoTheme.Colors.TextMuted;

            BlazeAuthorizationUrlResponse authorization =
                await _authenticationService.GenerateAuthorizationUrlAsync();

            Task<BlazeAuthenticationResult> loginTask =
                _authenticationService.CompleteLoginAsync();

            Process.Start(new ProcessStartInfo
            {
                FileName = authorization.Url,
                UseShellExecute = true
            });

            _detailsLabel.Text = "Blaze login opened. Waiting for approval...";
            _detailsLabel.ForeColor = WadevoTheme.Colors.Warning;

            BlazeAuthenticationResult result = await loginTask;

            if (!result.Success)
            {
                _detailsLabel.Text = result.Message;
                _detailsLabel.ForeColor = WadevoTheme.Colors.Error;
                return;
            }

            _detailsLabel.Text = $"Connected to Blaze. User ID: {_authenticationService.Connection.UserId}";
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

    private void RefreshStatus()
    {
        _eventsToggleButton.ButtonText = _liveEventService.IsListening ? "Stop Events" : "Start Events";

        if (_liveEventService.IsListening)
        {
            _statusLabel.Text = "🟣 Blaze Event Stream Connected";
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

        if (_authenticationService.PendingAuthorization != null)
        {
            _statusLabel.Text = "🟡 Login In Progress";
            _statusLabel.ForeColor = WadevoTheme.Colors.Warning;
            return;
        }

        _statusLabel.Text = "⚪ Ready to Connect";
        _statusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
    }
}
