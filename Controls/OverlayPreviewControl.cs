namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;
using Wadevo.Models;

public class OverlayPreviewControl : Control
{
    private OverlayThemeModel _theme = OverlayThemeModel.Neon();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public OverlayThemeModel Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PreviewTitle { get; set; } = "Song ID";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PreviewArtist { get; set; } = "Artist";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PreviewSong { get; set; } = "Song Title";

    public OverlayPreviewControl()
    {
        DoubleBuffered = true;
        Size = new Size(420, 120);
        BackColor = WadevoTheme.Colors.Background;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Color background = ColorTranslator.FromHtml(_theme.BackgroundHex);
        Color accent = ColorTranslator.FromHtml(_theme.AccentHex);
        Color text = ColorTranslator.FromHtml(_theme.TextHex);

        Rectangle bounds = new(4, 4, Width - 9, Height - 9);

        using GraphicsPath path = CreateRoundedRectangle(
            bounds,
            _theme.BorderRadius);

        if (_theme.ShowGlow)
        {
            using Pen glow = new(Color.FromArgb(70, accent), 7);
            e.Graphics.DrawPath(glow, path);
        }

        using SolidBrush backgroundBrush = new(background);
        e.Graphics.FillPath(backgroundBrush, path);

        using Pen border = new(accent, 2);
        e.Graphics.DrawPath(border, path);

        if (_theme.ShowArtwork)
        {
            using SolidBrush artworkBrush = new(accent);

            e.Graphics.FillEllipse(
                artworkBrush,
                22,
                28,
                52,
                52);
        }

        int textX = _theme.ShowArtwork ? 90 : 24;

        using SolidBrush accentBrush = new(accent);

        e.Graphics.DrawString(
            PreviewTitle,
            WadevoTheme.Fonts.Bold,
            accentBrush,
            textX,
            20);

        using SolidBrush textBrush = new(text);

        e.Graphics.DrawString(
            $"{PreviewArtist} - {PreviewSong}",
            WadevoTheme.Fonts.Default,
            textBrush,
            textX,
            50);

        if (_theme.ShowProgressBar)
        {
            using SolidBrush progressBrush = new(accent);

            e.Graphics.FillRectangle(
                progressBrush,
                textX,
                84,
                190,
                5);
        }
    }

    private static GraphicsPath CreateRoundedRectangle(
        Rectangle rectangle,
        int radius)
    {
        GraphicsPath path = new();

        int diameter = Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height));

        path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);

        path.CloseFigure();

        return path;
    }
}