namespace Wadevo.Modules.OverlayEngine;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

public sealed class OverlayThemeGallery : FlowLayoutPanel
{
    private readonly OverlayThemeService _themeService = new();

    public event Action<OverlayThemeModel>? ThemeSelected;

    public OverlayThemeGallery()
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        WrapContents = true;
        FlowDirection = FlowDirection.LeftToRight;
        BackColor = Color.Transparent;
        Padding = new Padding(6);

        LoadThemes();
    }

    public void ReloadThemes()
    {
        Controls.Clear();
        LoadThemes();
    }

    private void LoadThemes()
    {
        foreach (OverlayThemeModel theme in _themeService.GetThemes())
        {
            Controls.Add(CreateThemeCard(theme));
        }
    }

    private Control CreateThemeCard(OverlayThemeModel theme)
    {
        WadevoSelectionCard card = new()
        {
            Width = 250,
            Height = 198,
            Margin = new Padding(10)
        };

        OverlayPreviewControl preview = new()
        {
            Location = new Point(14, 14),
            Size = new Size(222, 86),
            Theme = theme,
            PreviewTitle = "Song ID",
            PreviewArtist = "Artist",
            PreviewSong = "Song Title"
        };

        Label title = new()
        {
            Text = theme.Name,
            Location = new Point(16, 110),
            Size = new Size(210, 20),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label description = new()
        {
            Text = theme.Description,
            Location = new Point(16, 132),
            Size = new Size(210, 42),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        card.Controls.Add(preview);
        card.Controls.Add(title);
        card.Controls.Add(description);

        card.Click += (_, _) => ThemeSelected?.Invoke(theme);
        preview.Click += (_, _) => ThemeSelected?.Invoke(theme);
        title.Click += (_, _) => ThemeSelected?.Invoke(theme);
        description.Click += (_, _) => ThemeSelected?.Invoke(theme);

        return card;
    }
}