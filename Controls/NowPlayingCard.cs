namespace Wadevo.Controls;

using Wadevo.Core;

public class NowPlayingCard : WadevoGlassCard
{
    private readonly AlbumArtControl _albumArt;
    private readonly Label _artistLabel;
    private readonly Label _songLabel;
    private readonly Label _hintLabel;

    public NowPlayingCard()
    {
        Dock = DockStyle.Top;
        Height = 220;
        AccentColor = WadevoTheme.Colors.Accent;

        Label sectionLabel = new()
        {
            Text = "♫  CURRENT SONG",
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Purple,
            Location = new Point(28, 24),
            Size = new Size(220, 28),
            BackColor = Color.Transparent
        };

        Label liveLabel = new()
        {
            Text = "● LIVE",
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Accent,
            Location = new Point(690, 24),
            Size = new Size(80, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _albumArt = new AlbumArtControl
        {
            Location = new Point(30, 68)
        };

        _artistLabel = new Label
        {
            Text = "Waiting for artist",
            Font = WadevoTheme.Fonts.Title,
            ForeColor = WadevoTheme.Colors.Text,
            Location = new Point(180, 64),
            Size = new Size(620, 70),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
        };

        _songLabel = new Label
        {
            Text = "Waiting for song",
            Font = WadevoTheme.Fonts.Header,
            ForeColor = WadevoTheme.Colors.Accent,
            Location = new Point(182, 138),
            Size = new Size(620, 42),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
        };

        _hintLabel = new Label
        {
            Text = "Live song data will appear here.",
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            Location = new Point(184, 176),
            Size = new Size(620, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
        };

        Controls.Add(sectionLabel);
        Controls.Add(liveLabel);
        Controls.Add(_albumArt);
        Controls.Add(_artistLabel);
        Controls.Add(_songLabel);
        Controls.Add(_hintLabel);
    }

    public void SetSong(string artist, string title)
    {
        _artistLabel.Text = string.IsNullOrWhiteSpace(artist)
            ? "Unknown Artist"
            : artist;

        _songLabel.Text = string.IsNullOrWhiteSpace(title)
            ? "Unknown Song"
            : title;

        _hintLabel.Text = "Updated from Serato live playlist.";
    }

    public void SetAlbumArt(Image image)
    {
        _albumArt.SetImage(image);
    }

    public void ClearAlbumArt()
    {
        _albumArt.ClearImage();
    }

    public void SetWaiting()
    {
        _artistLabel.Text = "Waiting for artist";
        _songLabel.Text = "Waiting for song";
        _hintLabel.Text = "Live song data will appear here.";

        _albumArt.ClearImage();
    }
}