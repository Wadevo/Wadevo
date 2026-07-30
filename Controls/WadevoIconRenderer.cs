namespace Wadevo.Controls;

using System.Drawing.Drawing2D;

public static class WadevoIconRenderer
{
    public static void Draw(Graphics graphics, WadevoIconKind kind, Rectangle bounds, Color color, float strokeWidth = 2f)
    {
        int size = Math.Min(bounds.Width, bounds.Height);

        if (size <= 0)
        {
            return;
        }

        float scale = size / 24f;

        GraphicsState state = graphics.Save();

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TranslateTransform(
            bounds.Left + (bounds.Width - size) / 2f,
            bounds.Top + (bounds.Height - size) / 2f);
        graphics.ScaleTransform(scale, scale);

        using Pen pen = new(color, strokeWidth / scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        using SolidBrush brush = new(color);

        switch (kind)
        {
            case WadevoIconKind.Connections:
                DrawConnections(graphics, pen, brush);
                break;

            case WadevoIconKind.Settings:
                DrawSettings(graphics, pen, brush);
                break;

            case WadevoIconKind.Blaze:
                DrawBlaze(graphics, brush);
                break;

            case WadevoIconKind.Alerts:
                DrawAlerts(graphics, pen, brush);
                break;

            case WadevoIconKind.OverlayEngine:
                DrawOverlayEngine(graphics, pen, brush);
                break;

            case WadevoIconKind.NowPlaying:
                DrawNowPlaying(graphics, pen, brush);
                break;

            case WadevoIconKind.Gifs:
                DrawGifs(graphics, pen, brush);
                break;

            case WadevoIconKind.Commands:
                DrawCommands(graphics, pen, brush);
                break;

            case WadevoIconKind.Dashboard:
                DrawDashboard(graphics, pen);
                break;

            case WadevoIconKind.GettingStarted:
                DrawGettingStarted(graphics, pen, brush);
                break;

            case WadevoIconKind.OverlayDesigner:
                DrawOverlayDesigner(graphics, pen);
                break;

            case WadevoIconKind.Donations:
                DrawDonations(graphics, pen, brush);
                break;

            case WadevoIconKind.SongRequests:
                DrawSongRequests(graphics, pen, brush);
                break;

            case WadevoIconKind.AssetLibrary:
                DrawAssetLibrary(graphics, pen, brush);
                break;

            case WadevoIconKind.WorkspaceStudio:
                DrawWorkspaceStudio(graphics, pen, brush);
                break;

            case WadevoIconKind.Soundboard:
                DrawSoundboard(graphics, pen, brush);
                break;
        }

        graphics.Restore(state);
    }

    private static void DrawConnections(Graphics g, Pen pen, SolidBrush brush)
    {
        g.DrawLine(pen, 6, 18, 18, 6);
        g.FillEllipse(brush, 3, 15, 6, 6);
        g.FillEllipse(brush, 15, 3, 6, 6);
    }

    private static void DrawSettings(Graphics g, Pen pen, SolidBrush brush)
    {
        g.DrawEllipse(pen, 6, 6, 12, 12);
        g.FillEllipse(brush, 10.5f, 10.5f, 3, 3);

        for (int i = 0; i < 8; i++)
        {
            double angle = i * Math.PI / 4;
            float cx = 12 + (float)(Math.Cos(angle) * 10);
            float cy = 12 + (float)(Math.Sin(angle) * 10);

            g.FillEllipse(brush, cx - 1.4f, cy - 1.4f, 2.8f, 2.8f);
        }
    }

    private static void DrawBlaze(Graphics g, SolidBrush brush)
    {
        using GraphicsPath flame = new();

        flame.AddBezier(new PointF(12, 2), new PointF(4, 10), new PointF(8, 14), new PointF(10, 12));
        flame.AddBezier(new PointF(10, 12), new PointF(8, 18), new PointF(14, 22), new PointF(14, 16));
        flame.AddBezier(new PointF(14, 16), new PointF(17, 19), new PointF(16, 10), new PointF(12, 2));
        flame.CloseFigure();

        g.FillPath(brush, flame);
    }

    private static void DrawAlerts(Graphics g, Pen pen, SolidBrush brush)
    {
        PointF[] triangle =
        {
            new(12, 3),
            new(21, 20),
            new(3, 20)
        };

        g.DrawPolygon(pen, triangle);
        g.DrawLine(pen, 12, 9, 12, 14);
        g.FillEllipse(brush, 10.7f, 16f, 2.6f, 2.6f);
    }

    private static void DrawOverlayEngine(Graphics g, Pen pen, SolidBrush brush)
    {
        g.FillEllipse(brush, 10, 10, 4, 4);
        g.DrawArc(pen, 6, 6, 12, 12, 220, 100);
        g.DrawArc(pen, 2, 2, 20, 20, 220, 100);
    }

    private static void DrawNowPlaying(Graphics g, Pen pen, SolidBrush brush)
    {
        g.FillEllipse(brush, 5, 16, 6, 5);
        g.DrawLine(pen, 11, 18, 11, 4);
        g.DrawLine(pen, 11, 4, 18, 6);
        g.DrawLine(pen, 18, 6, 18, 10);
    }

    private static void DrawGifs(Graphics g, Pen pen, SolidBrush brush)
    {
        using GraphicsPath frame = RoundedRectPath(new RectangleF(2, 4, 20, 16), 3);
        g.DrawPath(pen, frame);

        PointF[] play =
        {
            new(10, 9),
            new(10, 15),
            new(16, 12)
        };

        g.FillPolygon(brush, play);
    }

    private static void DrawCommands(Graphics g, Pen pen, SolidBrush brush)
    {
        using GraphicsPath bubble = RoundedRectPath(new RectangleF(3, 4, 18, 13), 4);
        g.DrawPath(pen, bubble);

        PointF[] tail =
        {
            new(8, 17),
            new(8, 21),
            new(12, 17)
        };

        g.FillPolygon(brush, tail);
    }

    private static void DrawOverlayDesigner(Graphics g, Pen pen)
    {
        using GraphicsPath backLayer = RoundedRectPath(new RectangleF(6, 2, 15, 15), 3);
        g.DrawPath(pen, backLayer);

        using GraphicsPath frontLayer = RoundedRectPath(new RectangleF(3, 7, 15, 15), 3);
        g.DrawPath(pen, frontLayer);
    }

    private static void DrawDonations(Graphics g, Pen pen, SolidBrush brush)
    {
        using GraphicsPath heart = new();

        heart.AddBezier(12, 20, 2, 12, 2, 5, 7, 4);
        heart.AddBezier(7, 4, 10, 4, 12, 7, 12, 9);
        heart.AddBezier(12, 9, 12, 7, 14, 4, 17, 4);
        heart.AddBezier(17, 4, 22, 5, 22, 12, 12, 20);
        heart.CloseFigure();

        g.DrawPath(pen, heart);
    }

    private static void DrawSongRequests(Graphics g, Pen pen, SolidBrush brush)
    {
        g.DrawLine(pen, 10, 4, 10, 17);
        g.DrawLine(pen, 10, 4, 19, 6);
        g.DrawLine(pen, 19, 6, 19, 17);

        g.FillEllipse(brush, 5, 15, 7, 6);
        g.FillEllipse(brush, 14, 15, 7, 6);
    }

    private static void DrawAssetLibrary(Graphics g, Pen pen, SolidBrush brush)
    {
        using GraphicsPath folder = RoundedRectPath(new RectangleF(3, 8, 18, 12), 2);
        g.DrawPath(pen, folder);

        g.DrawLine(pen, 3, 8, 8, 8);
        g.DrawLine(pen, 8, 8, 10, 5);
        g.DrawLine(pen, 10, 5, 16, 5);
        g.DrawLine(pen, 16, 5, 18, 8);
    }

    private static void DrawWorkspaceStudio(Graphics g, Pen pen, SolidBrush brush)
    {
        g.DrawRectangle(pen, 3, 3, 9, 9);
        g.DrawRectangle(pen, 14, 3, 7, 6);
        g.DrawRectangle(pen, 14, 11, 7, 9);
        g.DrawRectangle(pen, 3, 14, 9, 6);
    }

    private static void DrawSoundboard(Graphics g, Pen pen, SolidBrush brush)
    {
        using GraphicsPath padTopLeft = RoundedRectPath(new RectangleF(3, 3, 8, 8), 2);
        using GraphicsPath padTopRight = RoundedRectPath(new RectangleF(13, 3, 8, 8), 2);
        using GraphicsPath padBottomLeft = RoundedRectPath(new RectangleF(3, 13, 8, 8), 2);
        using GraphicsPath padBottomRight = RoundedRectPath(new RectangleF(13, 13, 8, 8), 2);

        g.DrawPath(pen, padTopLeft);
        g.DrawPath(pen, padTopRight);
        g.DrawPath(pen, padBottomLeft);

        // One pad filled solid to read as "pressed" - a soundboard is defined by
        // hitting a pad and something firing immediately, so one lit-up pad
        // communicates that at a glance better than four identical outlines.
        g.FillPath(brush, padBottomRight);
    }


    private static void DrawGettingStarted(Graphics g, Pen pen, SolidBrush brush)
    {
        g.DrawLine(pen, 6, 21, 6, 3);

        PointF[] flag =
        {
            new(7, 4),
            new(19, 8),
            new(7, 12)
        };

        g.FillPolygon(brush, flag);
    }

    private static void DrawDashboard(Graphics g, Pen pen)
    {
        DrawRoundedSquare(g, pen, 3, 3, 8);
        DrawRoundedSquare(g, pen, 13, 3, 8);
        DrawRoundedSquare(g, pen, 3, 13, 8);
        DrawRoundedSquare(g, pen, 13, 13, 8);
    }

    private static void DrawRoundedSquare(Graphics g, Pen pen, float x, float y, float size)
    {
        using GraphicsPath path = RoundedRectPath(new RectangleF(x, y, size, size), 2);
        g.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedRectPath(RectangleF rect, float radius)
    {
        GraphicsPath path = new();
        float diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));

        if (diameter <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
