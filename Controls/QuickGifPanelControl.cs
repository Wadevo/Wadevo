namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Gifs;

public sealed class QuickGifPanelControl : UserControl
{
    private readonly IGifProvider _gifProvider = new GiphyGifProvider();
    private readonly GifDownloadService _downloadService = new();
    private readonly GifSettingsService _settingsService = new();

    private readonly TextBox _searchBox = new();
    private readonly WadevoButton _searchButton = new();
    private readonly WadevoScrollablePanel _resultsPanel = new();
    private readonly Label _statusLabel = new();

    public QuickGifPanelControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        _searchBox.Location = new Point(8, 8);
        _searchBox.Size = new Size(180, 26);
        _searchBox.Font = WadevoTheme.Fonts.Default;
        _searchBox.ForeColor = WadevoTheme.Colors.Text;
        _searchBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.PlaceholderText = "Search GIFs…";

        _searchButton.ButtonText = "Search";
        _searchButton.Location = new Point(194, 6);
        _searchButton.Size = new Size(80, 30);
        _searchButton.AccentColor = WadevoTheme.Colors.Accent;
        _searchButton.ButtonClicked += async (_, _) => await RunSearchAsync();

        _searchBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await RunSearchAsync();
            }
        };

        _statusLabel.Location = new Point(8, 40);
        _statusLabel.Size = new Size(266, 20);
        _statusLabel.Font = WadevoTheme.Fonts.Small;
        _statusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _statusLabel.BackColor = Color.Transparent;

        _resultsPanel.Location = new Point(8, 64);
        _resultsPanel.BackColor = Color.Transparent;
        _resultsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Resize += (_, _) =>
        {
            _resultsPanel.Width = Math.Max(120, Width - 16);
            _resultsPanel.Height = Math.Max(80, Height - 72);
        };

        Controls.Add(_searchBox);
        Controls.Add(_searchButton);
        Controls.Add(_statusLabel);
        Controls.Add(_resultsPanel);
    }

    private async Task RunSearchAsync()
    {
        string query = _searchBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        GifSettingsModel settings = _settingsService.Load();

        if (string.IsNullOrWhiteSpace(settings.GiphyApiKey))
        {
            _statusLabel.Text = "Add a Giphy API key on the GIFs page first.";
            return;
        }

        _statusLabel.Text = "Searching...";
        _searchButton.Enabled = false;

        try
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));

            List<GifSearchResultModel> results = await _gifProvider.SearchAsync(
                query, settings.GiphyApiKey, 6, cts.Token);

            _resultsPanel.Content.SuspendLayout();

            foreach (Control control in _resultsPanel.Content.Controls.Cast<Control>().ToList())
            {
                _resultsPanel.Content.Controls.Remove(control);
                control.Dispose();
            }

            foreach (GifSearchResultModel gif in results)
            {
                _resultsPanel.Content.Controls.Add(BuildResultRow(gif));
            }

            _resultsPanel.Content.ResumeLayout();
            _resultsPanel.RefreshLayout();

            _statusLabel.Text = results.Count == 0 ? "No results." : $"{results.Count} results";
        }
        catch
        {
            _statusLabel.Text = "Search failed.";
        }
        finally
        {
            _searchButton.Enabled = true;
        }
    }

    private Panel BuildResultRow(GifSearchResultModel gif)
    {
        Panel row = new()
        {
            Width = 340,
            Height = 52,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = WadevoTheme.Colors.BackgroundSoft
        };

        PictureBox thumbnail = new()
        {
            Location = new Point(6, 6),
            Size = new Size(64, 40),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        if (!string.IsNullOrWhiteSpace(gif.PreviewUrl))
        {
            _ = LoadThumbnailAsync(thumbnail, gif.PreviewUrl);
        }

        Label titleLabel = new()
        {
            Text = string.IsNullOrWhiteSpace(gif.Title) ? "(untitled)" : gif.Title,
            Location = new Point(78, 16),
            Size = new Size(166, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        WadevoButton sendButton = new()
        {
            ButtonText = "Send",
            Location = new Point(252, 11),
            Size = new Size(80, 30),
            AccentColor = WadevoTheme.Colors.Success
        };

        sendButton.ButtonClicked += async (_, _) =>
        {
            sendButton.Enabled = false;

            try
            {
                string localPath = await _downloadService.DownloadAsync(gif);
                OverlayServer.TriggerGif(localPath, 5000, gif.Title);
                _statusLabel.Text = $"Sent \"{gif.Title}\"";
            }
            catch
            {
                _statusLabel.Text = "Send failed.";
            }
            finally
            {
                sendButton.Enabled = true;
            }
        };

        row.Controls.Add(thumbnail);
        row.Controls.Add(titleLabel);
        row.Controls.Add(sendButton);

        return row;
    }

    private static async Task LoadThumbnailAsync(PictureBox target, string previewUrl)
    {
        try
        {
            using HttpClient client = new();
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(8));

            byte[] bytes = await client.GetByteArrayAsync(previewUrl, cts.Token);

            if (target.IsDisposed)
            {
                return;
            }

            using MemoryStream stream = new(bytes);
            Image image = Image.FromStream(stream);

            if (!target.IsDisposed)
            {
                target.Image = image;
            }
        }
        catch
        {
            // Leave the placeholder (black box) if the thumbnail fails to load - a
            // missing preview image isn't worth surfacing as an error to the user.
        }
    }
}
