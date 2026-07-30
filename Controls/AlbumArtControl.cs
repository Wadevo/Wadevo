namespace Wadevo.Controls;

using Wadevo.Core;
using System.Drawing.Drawing2D;

public class AlbumArtControl : Control
{
    private Image? _albumArt;
    private string _artist = "";
    private string _title = "";

    public AlbumArtControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Size = new Size(120, 120);
        BackColor = WadevoTheme.Colors.Card;
    }

    public void SetImage(Image image)
    {
        _albumArt?.Dispose();
        _albumArt = (Image)image.Clone();
        Invalidate();
    }

    public void SetFallbackText(string artist, string title)
    {
        _artist = artist;
        _title = title;
        Invalidate();
    }

    public void ClearImage()
    {
        _albumArt?.Dispose();
        _albumArt = null;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Rectangle rect = new(0, 0, Width - 1, Height - 1);

        using GraphicsPath path = CreateRoundedRectangle(rect, 22);

        e.Graphics.SetClip(path);

        if (_albumArt != null)
        {
            e.Graphics.DrawImage(
                _albumArt,
                rect,
                new Rectangle(0, 0, _albumArt.Width, _albumArt.Height),
                GraphicsUnit.Pixel);
        }
        else
        {
            DrawGeneratedFallback(e.Graphics, rect);
        }

        e.Graphics.ResetClip();

        using Pen border = new(WadevoTheme.Colors.BorderGlow, 1);
        e.Graphics.DrawPath(border, path);
    }

    private void DrawGeneratedFallback(Graphics graphics, Rectangle rect)
    {
        Color start = PickColor(_artist, WadevoTheme.Colors.Purple);
        Color end = PickColor(_title, WadevoTheme.Colors.Accent);

        using LinearGradientBrush background = new(
            rect,
            start,
            end,
            LinearGradientMode.ForwardDiagonal);

        graphics.FillRectangle(background, rect);

        using SolidBrush overlay = new(Color.FromArgb(95, 0, 0, 0));
        graphics.FillRectangle(overlay, rect);

        string initials = GetInitials(_artist, _title);

        using Font initialsFont = new("Segoe UI", 30F, FontStyle.Bold);

        TextRenderer.DrawText(
            graphics,
            initials,
            initialsFont,
            rect,
            WadevoTheme.Colors.Text,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding);

        using Font iconFont = new("Segoe UI", 14F, FontStyle.Bold);

        Rectangle iconRect = new(
            rect.X,
            rect.Bottom - 30,
            rect.Width,
            24);

        TextRenderer.DrawText(
            graphics,
            "♫",
            iconFont,
            iconRect,
            Color.FromArgb(210, WadevoTheme.Colors.Text),
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter);
    }

    private static string GetInitials(string artist, string title)
    {
        string source = !string.IsNullOrWhiteSpace(artist)
            ? artist
            : title;

        if (string.IsNullOrWhiteSpace(source))
            return "♫";

        string[] words = source.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length == 0)
            return "♫";

        if (words.Length == 1)
            return words[0][0].ToString().ToUpperInvariant();

        return $"{words[0][0]}{words[^1][0]}".ToUpperInvariant();
    }

    private static Color PickColor(string text, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        int hash = Math.Abs(text.GetHashCode());

        Color[] palette =
        [
            WadevoTheme.Colors.Accent,
            WadevoTheme.Colors.Cyan,
            WadevoTheme.Colors.Purple,
            WadevoTheme.Colors.Pink,
            WadevoTheme.Colors.Orange
        ];

        return palette[hash % palette.Length];
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        GraphicsPath path = new();
        int diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _albumArt?.Dispose();
        }

        base.Dispose(disposing);
    }
}