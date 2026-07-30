namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

public sealed class SongRequestsModule : WadevoModule
{
    private readonly SongRequestService _service = WadevoSongRequestHub.SongRequestService;

    private readonly WadevoToggle _enabledToggle = new();
    private readonly WadevoTextBox _triggerWordBox = new();
    private readonly WadevoTextBox _maxQueueBox = new();
    private readonly WadevoTextBox _confirmationBox = new();
    private readonly WadevoToggle _postConfirmationToggle = new();

    private readonly WadevoScrollablePanel _queuePanel = new();
    private readonly Label _queueCountLabel = new();

    public SongRequestsModule()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        _service.QueueChanged += (_, _) =>
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(RefreshQueue));
            }
            else
            {
                RefreshQueue();
            }
        };

        WadevoGlassCard settingsCard = BuildSettingsCard();
        settingsCard.Dock = DockStyle.Top;
        settingsCard.Height = 260;

        Panel overlayPointerBanner = BuildOverlayPointerBanner();
        overlayPointerBanner.Dock = DockStyle.Top;
        overlayPointerBanner.Height = 46;

        WadevoGlassCard queueCard = BuildQueueCard();
        queueCard.Dock = DockStyle.Fill;

        Controls.Add(queueCard);
        Controls.Add(CreateSpacer());
        Controls.Add(settingsCard);
        Controls.Add(CreateSpacer());
        Controls.Add(overlayPointerBanner);

        RefreshQueue();
    }

    // The on-stream overlay used to be configured here, with its own fixed style. It now
    // lives as a real widget in the Overlay Designer instead - real font/color/size/
    // background control instead of one fixed look, so this just points people there
    // rather than duplicating a settings card that no longer does anything.
    private static Panel BuildOverlayPointerBanner()
    {
        Panel banner = new()
        {
            BackColor = WadevoTheme.Colors.BackgroundSoft
        };

        Label label = new()
        {
            Text = "📺 Want this on stream? Add a \"Song Queue\" widget in the Overlay Designer for full style control.",
            Location = new Point(8, 13),
            Size = new Size(700, 22),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        banner.Controls.Add(label);

        return banner;
    }

    private static Panel CreateSpacer()
    {
        return new Panel
        {
            Dock = DockStyle.Top,
            Height = 16,
            BackColor = Color.Transparent
        };
    }

    private WadevoGlassCard BuildSettingsCard()
    {
        SongRequestSettings settings = _service.GetSettings();

        WadevoGlassCard card = new()
        {
            AccentColor = WadevoTheme.Colors.Accent,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = "🎵 Song Requests",
            Location = new Point(24, 16),
            Size = new Size(300, 30),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent
        };

        _enabledToggle.Location = new Point(24, 58);
        _enabledToggle.IsOn = settings.IsEnabled;

        Label enabledLabel = new()
        {
            Text = "Accept song requests in chat",
            Location = new Point(114, 64),
            Size = new Size(260, 24),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label triggerLabel = new()
        {
            Text = "Trigger word",
            Location = new Point(24, 100),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _triggerWordBox.Location = new Point(24, 122);
        _triggerWordBox.Size = new Size(140, 38);
        _triggerWordBox.TextValue = settings.TriggerWord;

        Label maxQueueLabel = new()
        {
            Text = "Max in queue",
            Location = new Point(180, 100),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _maxQueueBox.Location = new Point(180, 122);
        _maxQueueBox.Size = new Size(100, 38);
        _maxQueueBox.TextValue = settings.MaxQueueSize.ToString();

        _postConfirmationToggle.Location = new Point(300, 128);
        _postConfirmationToggle.IsOn = settings.PostConfirmationToChat;

        Label confirmToggleLabel = new()
        {
            Text = "Confirm in chat",
            Location = new Point(390, 134),
            Size = new Size(180, 24),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label confirmationLabel = new()
        {
            Text = "Confirmation message (supports {song}, {username})",
            Location = new Point(24, 172),
            Size = new Size(500, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _confirmationBox.Location = new Point(24, 194);
        _confirmationBox.Size = new Size(620, 38);
        _confirmationBox.TextValue = settings.ConfirmationMessage;

        void SaveSettings()
        {
            SongRequestSettings current = _service.GetSettings();

            current.IsEnabled = _enabledToggle.IsOn;
            current.TriggerWord = string.IsNullOrWhiteSpace(_triggerWordBox.TextValue)
                ? "!sr"
                : _triggerWordBox.TextValue.Trim();
            current.MaxQueueSize = int.TryParse(_maxQueueBox.TextValue, out int max) && max > 0 ? max : 25;
            current.PostConfirmationToChat = _postConfirmationToggle.IsOn;
            current.ConfirmationMessage = _confirmationBox.TextValue;

            _service.SaveSettings(current);
        }

        _enabledToggle.IsOnChanged += (_, _) => SaveSettings();
        _postConfirmationToggle.IsOnChanged += (_, _) => SaveSettings();
        _triggerWordBox.TextValueChanged += (_, _) => SaveSettings();
        _maxQueueBox.TextValueChanged += (_, _) => SaveSettings();
        _confirmationBox.TextValueChanged += (_, _) => SaveSettings();

        card.Controls.Add(header);
        card.Controls.Add(_enabledToggle);
        card.Controls.Add(enabledLabel);
        card.Controls.Add(triggerLabel);
        card.Controls.Add(_triggerWordBox);
        card.Controls.Add(maxQueueLabel);
        card.Controls.Add(_maxQueueBox);
        card.Controls.Add(_postConfirmationToggle);
        card.Controls.Add(confirmToggleLabel);
        card.Controls.Add(confirmationLabel);
        card.Controls.Add(_confirmationBox);

        return card;
    }

    private WadevoGlassCard BuildQueueCard()
    {
        WadevoGlassCard card = new()
        {
            AccentColor = WadevoTheme.Colors.Purple,
            Padding = new Padding(24)
        };

        _queueCountLabel.Text = "📜 Queue";
        _queueCountLabel.Location = new Point(24, 16);
        _queueCountLabel.Size = new Size(300, 28);
        _queueCountLabel.Font = WadevoTheme.Fonts.CardHeader;
        _queueCountLabel.ForeColor = WadevoTheme.Colors.Purple;
        _queueCountLabel.BackColor = Color.Transparent;

        WadevoButton clearPlayedButton = new()
        {
            ButtonText = "Clear Played",
            Location = new Point(400, 18),
            Size = new Size(130, 32),
            AccentColor = WadevoTheme.Colors.TextMuted
        };

        clearPlayedButton.ButtonClicked += (_, _) => _service.ClearPlayed();

        WadevoButton clearAllButton = new()
        {
            ButtonText = "Clear All",
            Location = new Point(540, 18),
            Size = new Size(110, 32),
            AccentColor = WadevoTheme.Colors.Error
        };

        clearAllButton.ButtonClicked += (_, _) =>
        {
            bool confirmed = WadevoMessageBox.Confirm(
                FindForm(),
                "Clear the entire song request queue? This can't be undone.",
                "Clear Queue");

            if (confirmed)
            {
                _service.ClearAll();
            }
        };

        _queuePanel.Location = new Point(24, 58);
        _queuePanel.BackColor = Color.Transparent;
        _queuePanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _queuePanel.Content.Padding = new Padding(0, 0, 0, 12);

        card.Resize += (_, _) =>
        {
            _queuePanel.Width = Math.Max(120, card.ClientSize.Width - 48);
            _queuePanel.Height = Math.Max(120, card.ClientSize.Height - 82);
        };

        card.Controls.Add(_queueCountLabel);
        card.Controls.Add(clearPlayedButton);
        card.Controls.Add(clearAllButton);
        card.Controls.Add(_queuePanel);

        return card;
    }

    private void RefreshQueue()
    {
        _queuePanel.Content.SuspendLayout();

        foreach (Control control in _queuePanel.Content.Controls.Cast<Control>().ToList())
        {
            _queuePanel.Content.Controls.Remove(control);
            control.Dispose();
        }

        IReadOnlyList<SongRequestModel> queue = _service.GetQueue()
            .OrderBy(r => r.IsPlayed)
            .ThenBy(r => r.RequestedAtUtc)
            .ToList();

        _queueCountLabel.Text = $"📜 Queue ({queue.Count(r => !r.IsPlayed)} pending)";

        int itemWidth = Math.Max(200, _queuePanel.ClientSize.Width - 20);

        foreach (SongRequestModel request in queue)
        {
            _queuePanel.Content.Controls.Add(BuildQueueRow(request, itemWidth));
        }

        if (queue.Count == 0)
        {
            Label empty = new()
            {
                Text = "No requests yet. Chat can use the trigger word to add one.",
                Size = new Size(itemWidth, 40),
                Font = WadevoTheme.Fonts.Default,
                ForeColor = WadevoTheme.Colors.TextMuted,
                BackColor = Color.Transparent
            };

            _queuePanel.Content.Controls.Add(empty);
        }

        _queuePanel.Content.ResumeLayout();
        _queuePanel.RefreshLayout();
    }

    private Panel BuildQueueRow(SongRequestModel request, int width)
    {
        Panel row = new()
        {
            Width = width,
            Height = 60,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = WadevoTheme.Colors.BackgroundSoft
        };

        Label songLabel = new()
        {
            Text = request.SongText,
            Location = new Point(12, 8),
            Size = new Size(width - 220, 22),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = request.IsPlayed ? WadevoTheme.Colors.TextMuted : WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label requesterLabel = new()
        {
            Text = $"requested by {request.RequesterUsername}",
            Location = new Point(12, 32),
            Size = new Size(width - 220, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoButton playedButton = new()
        {
            ButtonText = request.IsPlayed ? "✅ Played" : "Mark Played",
            Location = new Point(width - 200, 14),
            Size = new Size(110, 32),
            AccentColor = request.IsPlayed ? WadevoTheme.Colors.TextMuted : WadevoTheme.Colors.Success,
            Enabled = !request.IsPlayed
        };

        playedButton.ButtonClicked += (_, _) => _service.MarkPlayed(request.Id);

        WadevoButton removeButton = new()
        {
            ButtonText = "✕",
            Location = new Point(width - 84, 14),
            Size = new Size(40, 32),
            AccentColor = WadevoTheme.Colors.Error
        };

        removeButton.ButtonClicked += (_, _) => _service.Remove(request.Id);

        row.Controls.Add(songLabel);
        row.Controls.Add(requesterLabel);
        row.Controls.Add(playedButton);
        row.Controls.Add(removeButton);

        return row;
    }
}
