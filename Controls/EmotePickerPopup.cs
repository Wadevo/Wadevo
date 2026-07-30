namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

/// <summary>
/// A searchable grid of emotes (BTTV global + the user's own custom uploads) that inserts
/// ":shortcode:" into whichever text box it was opened from. Same interaction pattern as
/// the GIF picker - search, click a tile, done - plus a built-in way to upload/remove
/// custom emotes so that doesn't need its own separate settings page.
/// </summary>
public static class EmotePickerPopup
{
    private static readonly HttpClient ThumbnailHttpClient = new();

    public static void ShowFor(Control anchor, TextBox targetTextBox)
    {
        Panel content = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WadevoTheme.Colors.Background,
            Padding = new Padding(16)
        };

        Label header = new()
        {
            Text = "😀 Insert Emote",
            Location = new Point(16, 12),
            Size = new Size(240, 26),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent
        };

        TextBox searchBox = new()
        {
            Location = new Point(16, 44),
            Size = new Size(300, 30),
            PlaceholderText = "Search emotes…",
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = WadevoTheme.Colors.BackgroundSoft,
            BorderStyle = BorderStyle.FixedSingle
        };

        WadevoButton uploadButton = new()
        {
            ButtonText = "+ Upload",
            Location = new Point(324, 44),
            Size = new Size(96, 30),
            AccentColor = WadevoTheme.Colors.Success
        };

        WadevoScrollablePanel resultsPanel = new()
        {
            Location = new Point(16, 84),
            Size = new Size(404, 340),
            BackColor = Color.Transparent
        };

        resultsPanel.Content.WrapContents = true;
        resultsPanel.Content.FlowDirection = FlowDirection.LeftToRight;
        resultsPanel.Content.Padding = new Padding(0, 4, 0, 4);

        Label emptyLabel = new()
        {
            Text = "No emotes match that search.",
            Size = new Size(360, 30),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        content.Controls.Add(header);
        content.Controls.Add(searchBox);
        content.Controls.Add(uploadButton);
        content.Controls.Add(resultsPanel);

        WadevoDropdownPopup popup = new(content, 440, 450);

        void RefreshGrid(string search)
        {
            resultsPanel.Content.SuspendLayout();

            foreach (Control control in resultsPanel.Content.Controls.Cast<Control>().ToList())
            {
                resultsPanel.Content.Controls.Remove(control);
                control.Dispose();
            }

            IEnumerable<EmoteModel> emotes = EmoteCache.GetSnapshot().Values
                .OrderByDescending(e => e.IsCustom)
                .ThenBy(e => e.Shortcode, StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(search))
            {
                emotes = emotes.Where(e => e.Shortcode.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            List<EmoteModel> emoteList = emotes.ToList();

            if (emoteList.Count == 0)
            {
                resultsPanel.Content.Controls.Add(emptyLabel);
                resultsPanel.RefreshLayout();
                resultsPanel.Content.ResumeLayout();
                return;
            }

            foreach (EmoteModel emote in emoteList)
            {
                resultsPanel.Content.Controls.Add(CreateEmoteTile(emote, targetTextBox, popup));
            }

            resultsPanel.Content.ResumeLayout();
            resultsPanel.RefreshLayout();
        }

        searchBox.TextChanged += (_, _) => RefreshGrid(searchBox.Text.Trim());

        uploadButton.ButtonClicked += (_, _) =>
        {
            UploadCustomEmote(popup);
            RefreshGrid(searchBox.Text.Trim());
        };

        RefreshGrid("");

        popup.ShowBelow(anchor);
    }

    private static Control CreateEmoteTile(EmoteModel emote, TextBox targetTextBox, WadevoDropdownPopup popup)
    {
        Panel tile = new()
        {
            Size = new Size(84, 84),
            Margin = new Padding(4),
            BackColor = WadevoTheme.Colors.BackgroundSoft,
            Cursor = Cursors.Hand
        };

        PictureBox thumbnail = new()
        {
            Location = new Point(8, 8),
            Size = new Size(68, 44),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        Label codeLabel = new()
        {
            Text = emote.Shortcode,
            Location = new Point(4, 56),
            Size = new Size(76, 16),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = emote.IsCustom ? WadevoTheme.Colors.Accent : WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true
        };

        void InsertShortcode()
        {
            string token = $":{emote.Shortcode}:";
            int caret = targetTextBox.SelectionStart;

            targetTextBox.Text = targetTextBox.Text.Insert(caret, token);
            targetTextBox.SelectionStart = caret + token.Length;
            targetTextBox.Focus();

            popup.Close();
        }

        tile.Click += (_, _) => InsertShortcode();
        thumbnail.Click += (_, _) => InsertShortcode();
        codeLabel.Click += (_, _) => InsertShortcode();

        tile.Controls.Add(thumbnail);
        tile.Controls.Add(codeLabel);

        LoadThumbnailAsync(thumbnail, emote);

        return tile;
    }

    private static async void LoadThumbnailAsync(PictureBox thumbnail, EmoteModel emote)
    {
        try
        {
            if (emote.IsCustom)
            {
                // Custom emotes are stored as local files - ImageUrl is the web-facing
                // /media?path=... form used by overlays, not useful for direct WinForms
                // loading, so this reads the shortcode back through CustomEmoteService to
                // get the actual file path instead.
                CustomEmoteModel? stored = new CustomEmoteService().LoadAll()
                    .FirstOrDefault(e => string.Equals(e.Shortcode, emote.Shortcode, StringComparison.OrdinalIgnoreCase));

                if (stored is null || !File.Exists(stored.FilePath) || thumbnail.IsDisposed)
                {
                    return;
                }

                using FileStream fileStream = new(stored.FilePath, FileMode.Open, FileAccess.Read);
                MemoryStream buffer = new();
                await fileStream.CopyToAsync(buffer);

                if (thumbnail.IsDisposed)
                {
                    return;
                }

                thumbnail.Image = Image.FromStream(buffer);
                return;
            }

            byte[] bytes = await ThumbnailHttpClient.GetByteArrayAsync(emote.ImageUrl);

            if (thumbnail.IsDisposed)
            {
                return;
            }

            thumbnail.Image = Image.FromStream(new MemoryStream(bytes));
        }
        catch
        {
            // A failed thumbnail just leaves the tile blank - not worth surfacing an error
            // for one emote out of a whole grid.
        }
    }

    private static void UploadCustomEmote(WadevoDropdownPopup popup)
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Upload custom emote",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif"
        };

        if (dialog.ShowDialog(popup) != DialogResult.OK)
        {
            return;
        }

        string suggestedShortcode = CustomEmoteService.NormalizeShortcode(
            Path.GetFileNameWithoutExtension(dialog.FileName));

        using WadevoTextPromptForm promptForm = new(
            "Name This Emote",
            "Shortcode (letters, numbers, underscore only - no spaces or colons)",
            suggestedShortcode);

        if (promptForm.ShowDialog(popup) != DialogResult.OK)
        {
            return;
        }

        try
        {
            new CustomEmoteService().Add(promptForm.InputText, dialog.FileName);
            _ = EmoteCache.RefreshAsync();
        }
        catch (Exception ex)
        {
            WadevoMessageBox.Show(popup, $"Couldn't add that emote: {ex.Message}", "Wadevo");
        }
    }
}
