namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Gifs;

public sealed class GifsModule : WadevoModule
{
    private static readonly HttpClient ThumbnailHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private readonly GifSettingsService _settingsService = new();
    private readonly GifDownloadService _downloadService = new();

    private readonly GiphyGifProvider _provider = new();

    private readonly Panel _toolbarPanel = new();
    private readonly Panel _settingsPanel = new();
    private readonly Label _statusLabel = new();
    private readonly WadevoScrollablePanel _resultsPanel = new();

    private readonly TextBox _searchTextBox = new();
    private readonly WadevoButton _searchButton = new();
    private readonly NumericUpDown _durationInput = new();
    private readonly WadevoButton _settingsToggleButton = new();

    private readonly TextBox _giphyKeyTextBox = new();
    private readonly WadevoButton _saveSettingsButton = new();

    private GifSettingsModel _settings;
    private CancellationTokenSource? _searchCts;
    private readonly List<MemoryStream> _thumbnailStreams = new();

    public override string ModuleName => "GIFs";

    public override string ModuleDescription =>
        "Search Giphy, then send any GIF straight to your stream.";

    public GifsModule()
    {
        BackColor = WadevoTheme.Colors.Background;
        Padding = new Padding(0);
        AutoScroll = false;
        Dock = DockStyle.Fill;

        _settings = _settingsService.Load();

        BuildToolbar();
        BuildSettingsPanel();
        BuildStatusLabel();
        BuildResultsPanel();

        Controls.Add(_resultsPanel);
        Controls.Add(_statusLabel);
        Controls.Add(_settingsPanel);
        Controls.Add(_toolbarPanel);

        Disposed += (_, _) =>
        {
            _searchCts?.Cancel();
            DisposeThumbnailStreams();
        };

        RunSearch("");
    }

    private void BuildToolbar()
    {
        _toolbarPanel.Dock = DockStyle.Top;
        _toolbarPanel.Height = 64;
        _toolbarPanel.BackColor = Color.Transparent;

        _searchTextBox.Location = new Point(0, 16);
        _searchTextBox.Size = new Size(300, 32);
        _searchTextBox.Font = WadevoTheme.Fonts.Default;
        _searchTextBox.ForeColor = WadevoTheme.Colors.Text;
        _searchTextBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _searchTextBox.BorderStyle = BorderStyle.FixedSingle;
        _searchTextBox.PlaceholderText = "Search GIFs…";
        _searchTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                RunSearch(_searchTextBox.Text.Trim());
            }
        };

        _searchButton.ButtonText = "Search";
        _searchButton.Location = new Point(310, 12);
        _searchButton.Size = new Size(96, 40);
        _searchButton.ButtonClicked += (_, _) => RunSearch(_searchTextBox.Text.Trim());

        Label durationLabel = new()
        {
            Text = "Duration",
            Location = new Point(422, 8),
            Size = new Size(70, 18),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _durationInput.Location = new Point(422, 28);
        _durationInput.Size = new Size(64, 28);
        _durationInput.Minimum = 1;
        _durationInput.Maximum = 60;
        _durationInput.Value = Math.Clamp(_settings.DefaultDurationSeconds, 1, 60);
        _durationInput.Font = WadevoTheme.Fonts.Default;
        _durationInput.TextAlign = HorizontalAlignment.Center;

        Label durationUnitLabel = new()
        {
            Text = "sec",
            Location = new Point(490, 32),
            Size = new Size(30, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _settingsToggleButton.ButtonText = "API Key";
        _settingsToggleButton.Location = new Point(534, 12);
        _settingsToggleButton.Size = new Size(100, 40);
        _settingsToggleButton.ButtonClicked += (_, _) => ToggleSettingsPanel();

        _toolbarPanel.Controls.Add(_searchTextBox);
        _toolbarPanel.Controls.Add(_searchButton);
        _toolbarPanel.Controls.Add(durationLabel);
        _toolbarPanel.Controls.Add(_durationInput);
        _toolbarPanel.Controls.Add(durationUnitLabel);
        _toolbarPanel.Controls.Add(_settingsToggleButton);
    }

    private void BuildSettingsPanel()
    {
        _settingsPanel.Dock = DockStyle.Top;
        _settingsPanel.Height = 0;
        _settingsPanel.Visible = false;
        _settingsPanel.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _settingsPanel.Padding = new Padding(16);

        Label giphyLabel = new()
        {
            Text = "Giphy API Key",
            Location = new Point(16, 12),
            Size = new Size(200, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _giphyKeyTextBox.Location = new Point(16, 34);
        _giphyKeyTextBox.Size = new Size(320, 30);
        _giphyKeyTextBox.Font = WadevoTheme.Fonts.Default;
        _giphyKeyTextBox.ForeColor = WadevoTheme.Colors.Text;
        _giphyKeyTextBox.BackColor = WadevoTheme.Colors.Background;
        _giphyKeyTextBox.BorderStyle = BorderStyle.FixedSingle;
        _giphyKeyTextBox.UseSystemPasswordChar = true;
        _giphyKeyTextBox.Text = _settings.GiphyApiKey;

        _saveSettingsButton.ButtonText = "Save Key";
        _saveSettingsButton.Location = new Point(354, 30);
        _saveSettingsButton.Size = new Size(110, 38);
        _saveSettingsButton.ButtonClicked += (_, _) => SaveSettings();

        Label hintLabel = new()
        {
            Text = "Get a free key at developers.giphy.com — your key is stored locally on this PC.",
            Location = new Point(16, 70),
            Size = new Size(760, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _settingsPanel.Controls.Add(giphyLabel);
        _settingsPanel.Controls.Add(_giphyKeyTextBox);
        _settingsPanel.Controls.Add(_saveSettingsButton);
        _settingsPanel.Controls.Add(hintLabel);
    }

    private void BuildStatusLabel()
    {
        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Height = 26;
        _statusLabel.Text = "Ready.";
        _statusLabel.Font = WadevoTheme.Fonts.Small;
        _statusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
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
    }

    private void ToggleSettingsPanel()
    {
        ShowSettingsPanel(!_settingsPanel.Visible);
    }

    private void ShowSettingsPanel(bool visible)
    {
        _settingsPanel.Visible = visible;
        _settingsPanel.Height = visible ? 108 : 0;
    }

    private void SaveSettings()
    {
        _settings.GiphyApiKey = _giphyKeyTextBox.Text.Trim();
        _settings.DefaultDurationSeconds = (int)_durationInput.Value;

        _settingsService.Save(_settings);

        SetStatus("GIF settings saved.", WadevoTheme.Colors.Success);

        RunSearch(_searchTextBox.Text.Trim());
    }

    private async void RunSearch(string query)
    {
        _searchCts?.Cancel();

        CancellationTokenSource cts = new();
        _searchCts = cts;

        string apiKey = _settings.GiphyApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            SetStatus("Add your Giphy API key below, then search.", WadevoTheme.Colors.Warning);
            ShowSettingsPanel(true);
            ClearResults();
            return;
        }

        SetStatus("Searching Giphy…", WadevoTheme.Colors.TextMuted);

        try
        {
            List<GifSearchResultModel> results = string.IsNullOrWhiteSpace(query)
                ? await _provider.GetTrendingAsync(apiKey, 24, cts.Token)
                : await _provider.SearchAsync(query, apiKey, 24, cts.Token);

            if (cts.Token.IsCancellationRequested || IsDisposed)
            {
                return;
            }

            ClearResults();

            if (results.Count == 0)
            {
                SetStatus("No GIFs found. Try a different search.", WadevoTheme.Colors.TextMuted);
                return;
            }

            foreach (GifSearchResultModel gif in results)
            {
                _resultsPanel.Content.Controls.Add(CreateResultCard(gif, cts.Token));
            }

            _resultsPanel.RefreshLayout();

            string queryLabel = string.IsNullOrWhiteSpace(query) ? "trending" : $"\"{query}\"";
            SetStatus($"{results.Count} results for {queryLabel} on Giphy.", WadevoTheme.Colors.Success);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                WadevoLogger.Error("Giphy search failed", ex);

                string summary = ex.Message.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? ex.Message;
                SetStatus($"{summary} (full details in wadevo.log)", WadevoTheme.Colors.Error);
            }
        }
    }

    private Control CreateResultCard(GifSearchResultModel gif, CancellationToken token)
    {
        WadevoCard card = new()
        {
            Width = 186,
            Height = 246,
            Margin = new Padding(8),
            Padding = new Padding(0),
            ShowAccent = false,
            BackColor = WadevoTheme.Colors.Card
        };

        AnimatedGifPictureBox preview = new()
        {
            Location = new Point(8, 8),
            Size = new Size(170, 128),
            BackColor = WadevoTheme.Colors.BackgroundSoft
        };

        Label sourceBadge = new()
        {
            Text = gif.Source,
            Location = new Point(8, 142),
            Size = new Size(170, 16),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        Label titleLabel = new()
        {
            Text = gif.Title,
            Location = new Point(8, 160),
            Size = new Size(170, 34),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        WadevoButton sendButton = new()
        {
            ButtonText = "Send to Stream",
            Location = new Point(8, 200),
            Size = new Size(170, 36),
            AccentColor = WadevoTheme.Colors.Accent
        };

        sendButton.ButtonClicked += (_, _) => SendToStream(gif, sendButton);

        card.Controls.Add(preview);
        card.Controls.Add(sourceBadge);
        card.Controls.Add(titleLabel);
        card.Controls.Add(sendButton);

        LoadThumbnailAsync(preview, gif.PreviewUrl, token);

        return card;
    }

    private async void LoadThumbnailAsync(
        AnimatedGifPictureBox preview,
        string url,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            byte[] bytes = await ThumbnailHttpClient.GetByteArrayAsync(url, token);

            if (token.IsCancellationRequested || preview.IsDisposed || IsDisposed)
            {
                return;
            }

            MemoryStream stream = new(bytes);
            Image image = Image.FromStream(stream);

            if (preview.IsDisposed || IsDisposed)
            {
                image.Dispose();
                stream.Dispose();
                return;
            }

            lock (_thumbnailStreams)
            {
                _thumbnailStreams.Add(stream);
            }

            preview.SetImage(image);
        }
        catch
        {
            // A failed thumbnail just leaves the placeholder background; not worth surfacing.
        }
    }

    private async void SendToStream(GifSearchResultModel gif, WadevoButton sendButton)
    {
        sendButton.Enabled = false;
        string originalText = sendButton.ButtonText;
        sendButton.ButtonText = "Sending…";

        SetStatus($"Downloading \"{gif.Title}\"…", WadevoTheme.Colors.Warning);

        try
        {
            string localPath = await _downloadService.DownloadAsync(gif);
            int durationSeconds = (int)_durationInput.Value;

            OverlayServer.TriggerGif(localPath, durationSeconds * 1000, gif.Title);

            SetStatus(
                $"✓ Sent \"{gif.Title}\" to the /gifs overlay for {durationSeconds}s.",
                WadevoTheme.Colors.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"Couldn't send that GIF: {ex.Message}", WadevoTheme.Colors.Error);
        }
        finally
        {
            if (!IsDisposed && !sendButton.IsDisposed)
            {
                sendButton.Enabled = true;
                sendButton.ButtonText = originalText;
            }
        }
    }

    private void ClearResults()
    {
        Control[] oldControls = _resultsPanel.Content.Controls.Cast<Control>().ToArray();

        _resultsPanel.Content.Controls.Clear();
        _resultsPanel.RefreshLayout();

        foreach (Control control in oldControls)
        {
            control.Dispose();
        }
    }

    private void DisposeThumbnailStreams()
    {
        lock (_thumbnailStreams)
        {
            foreach (MemoryStream stream in _thumbnailStreams)
            {
                stream.Dispose();
            }

            _thumbnailStreams.Clear();
        }
    }

    private void SetStatus(string text, Color color)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(text, color));
            return;
        }

        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }
}
