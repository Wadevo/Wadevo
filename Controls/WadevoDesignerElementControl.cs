namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public sealed class WadevoDesignerElementControl : Panel
{
    private const int HandleSize = 10;

    private static readonly Dictionary<string, Image> ArtworkCache = new();
    private static readonly Dictionary<string, Image> ImageFileCache = new();
    private static readonly HttpClient ArtworkHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly Label _textLabel = new();
    private Font? _ownedFont;

    public WadevoDesignerElementState State { get; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsSelected { get; set; }

    public WadevoDesignerElementControl(WadevoDesignerElementState state)
    {
        State = state;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        BackColor = Color.Transparent;
        Cursor = Cursors.SizeAll;

        _textLabel.Dock = DockStyle.Fill;
        _textLabel.BackColor = Color.Transparent;
        _textLabel.ForeColor = WadevoTheme.Colors.Text;
        _textLabel.Font = WadevoTheme.Fonts.Default;
        _textLabel.TextAlign = ContentAlignment.MiddleLeft;
        _textLabel.Padding = new Padding(10, 0, 10, 0);

        Controls.Add(_textLabel);

        RefreshFromState();
    }

    public void RefreshFromState()
    {
        if (Parent is WadevoDesignerCanvas canvas)
        {
            Bounds = new Rectangle(
                (int)(State.Bounds.X * canvas.RenderScaleX),
                (int)(State.Bounds.Y * canvas.RenderScaleY),
                (int)(State.Bounds.Width * canvas.RenderScaleX),
                (int)(State.Bounds.Height * canvas.RenderScaleY));
        }
        else
        {
            Bounds = State.Bounds;
        }

        Visible = State.IsVisible;

        bool hasArtworkImage = State.Kind == WadevoDesignerElementKind.Artwork &&
                                !string.IsNullOrWhiteSpace(State.ArtworkUrl);

        bool hasImageFile = State.Kind == WadevoDesignerElementKind.Image &&
                             !string.IsNullOrWhiteSpace(State.ImagePath);

        _textLabel.Visible = State.Kind is
            WadevoDesignerElementKind.PreviewSurface or
            WadevoDesignerElementKind.Text or
            WadevoDesignerElementKind.ProgressBar or
            WadevoDesignerElementKind.Group or
            WadevoDesignerElementKind.Clock or
            WadevoDesignerElementKind.Countdown or
            WadevoDesignerElementKind.SongQueue or
            WadevoDesignerElementKind.VoteTally or
            WadevoDesignerElementKind.ChatFeed
            || (State.Kind == WadevoDesignerElementKind.Artwork && !hasArtworkImage)
            || (State.Kind == WadevoDesignerElementKind.Image && !hasImageFile);

        _textLabel.Text = State.Kind switch
        {
            WadevoDesignerElementKind.PreviewSurface => State.Text,
            WadevoDesignerElementKind.Text => State.Text,
            WadevoDesignerElementKind.ProgressBar => $"📈 {State.GoalMetric} goal ({(State.GoalPlatform == "All" ? "All Platforms" : State.GoalPlatform)}): 0 / {State.GoalTarget}",
            WadevoDesignerElementKind.Artwork => "Artwork",
            WadevoDesignerElementKind.Image => "Image",
            WadevoDesignerElementKind.Shape => "",
            WadevoDesignerElementKind.Group => "Group",
            WadevoDesignerElementKind.Clock => "🕐 " + FormatClockPreview(State.ClockFormat),
            WadevoDesignerElementKind.Countdown => "⏱ Live countdown",
            WadevoDesignerElementKind.SongQueue => $"🎵 Song Requests (live, up to {State.SongQueueMaxVisible})",
            WadevoDesignerElementKind.ChatFeed => "💬 Combined Chat (live - configure in Overlay Engine)",
            _ => State.Text
        };

        Font? previousOwnedFont = _ownedFont;

        _ownedFont = Wadevo.Services.CustomFontService.CreateFont(
            State.FontFamily,
            State.FontSize,
            State.FontBold ? FontStyle.Bold : FontStyle.Regular);

        _textLabel.Font = _ownedFont;
        _textLabel.ForeColor = Color.FromArgb(State.FontColorArgb);

        previousOwnedFont?.Dispose();

        if (hasArtworkImage)
        {
            EnsureArtworkLoaded(State.ArtworkUrl);
        }

        if (hasImageFile)
        {
            EnsureImageFileLoaded(State.ImagePath);
        }

        Invalidate();
        Update();
    }

    private static string FormatClockPreview(string format)
    {
        try
        {
            return DateTime.Now.ToString(string.IsNullOrWhiteSpace(format) ? "h:mm tt" : format);
        }
        catch
        {
            return DateTime.Now.ToString("h:mm tt");
        }
    }

    private void EnsureArtworkLoaded(string url)
    {
        if (ArtworkCache.ContainsKey(url))
        {
            return;
        }

        _ = LoadArtworkAsync(url);
    }

    private void EnsureImageFileLoaded(string path)
    {
        if (ImageFileCache.ContainsKey(path))
        {
            return;
        }

        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            // Load via a memory stream (not Image.FromFile) so the file handle isn't held
            // open for the lifetime of the Image - the source file could get replaced later.
            byte[] bytes = File.ReadAllBytes(path);
            using MemoryStream stream = new(bytes);
            Image image = Image.FromStream(stream);

            ImageFileCache[path] = image;
        }
        catch
        {
            // If the image can't be loaded, the placeholder is shown instead.
        }
    }

    private async Task LoadArtworkAsync(string url)
    {
        try
        {
            byte[] bytes = await ArtworkHttpClient.GetByteArrayAsync(url);

            using MemoryStream stream = new(bytes);
            Image image = Image.FromStream(stream);

            ArtworkCache[url] = image;

            if (!IsDisposed && State.ArtworkUrl == url)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new MethodInvoker(RefreshFromState));
                }
                else
                {
                    RefreshFromState();
                }
            }
        }
        catch
        {
            // If artwork can't be loaded, the placeholder is shown instead.
        }
    }

    public void SyncStateFromControl()
    {
        State.Bounds = Bounds;
        State.IsVisible = Visible;
    }

    public WadevoDesignerResizeHandle HitTestHandle(Point clientPoint)
    {
        if (!IsSelected)
        {
            return WadevoDesignerResizeHandle.None;
        }

        foreach ((WadevoDesignerResizeHandle handle, Rectangle rect) in GetHandleRectangles())
        {
            if (rect.Contains(clientPoint))
            {
                return handle;
            }
        }

        return WadevoDesignerResizeHandle.None;
    }

    private IEnumerable<(WadevoDesignerResizeHandle Handle, Rectangle Rect)> GetHandleRectangles()
    {
        int size = HandleSize;

        yield return (WadevoDesignerResizeHandle.TopLeft, new Rectangle(0, 0, size, size));
        yield return (WadevoDesignerResizeHandle.TopRight, new Rectangle(Math.Max(0, Width - size), 0, size, size));
        yield return (WadevoDesignerResizeHandle.BottomLeft, new Rectangle(0, Math.Max(0, Height - size), size, size));
        yield return (WadevoDesignerResizeHandle.BottomRight, new Rectangle(Math.Max(0, Width - size), Math.Max(0, Height - size), size, size));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Previously left empty to avoid a black-box artifact behind the transparent-
        // background text label on top of this control. That trade was wrong - suppressing
        // the erase step entirely meant this control's region was NEVER cleared before
        // drawing, so whatever had previously occupied that same screen space (from any
        // other control, dialog, or page) could remain visible underneath the new content.
        // Explicitly clearing to a real, correctly-resolved color fixes both problems: no
        // black box, and no stale content bleeding through from elsewhere.
        e.Graphics.Clear(FindOpaqueBackColor());
    }

    private Color FindOpaqueBackColor()
    {
        Control? current = Parent;

        while (current is not null)
        {
            if (current.BackColor != Color.Transparent)
            {
                return current.BackColor;
            }

            current = current.Parent;
        }

        return WadevoTheme.Colors.Background;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        PaintElementBackground(e.Graphics, bounds);
        PaintElementContent(e.Graphics, bounds);
        PaintElementBorder(e.Graphics, bounds);

        if (IsSelected)
        {
            PaintResizeHandles(e.Graphics);
        }
    }

    private void PaintResizeHandles(Graphics graphics)
    {
        using SolidBrush handleBrush = new(WadevoDesignerColors.Selection);
        using Pen handleBorderPen = new(Color.White, 1);

        foreach ((_, Rectangle rect) in GetHandleRectangles())
        {
            graphics.FillEllipse(handleBrush, rect);
            graphics.DrawEllipse(handleBorderPen, rect);
        }
    }

    private void PaintElementBackground(Graphics graphics, Rectangle bounds)
    {
        if (State.Kind == WadevoDesignerElementKind.Text)
        {
            // A filled box behind every piece of text made it hard to actually see what
            // you were designing - a thin outline is enough to show the element's bounds
            // while editing, without obscuring the text itself.
            using GraphicsPath outlinePath = CreateRoundedRectangle(bounds, 10);
            using Pen outlinePen = new(WadevoDesignerColors.ElementFill, 1.5f);

            graphics.DrawPath(outlinePen, outlinePath);
            return;
        }

        Color fillColor = State.Kind == WadevoDesignerElementKind.PreviewSurface
            ? WadevoDesignerColors.SurfaceBackground
            : WadevoDesignerColors.ElementFill;

        using GraphicsPath path = CreateRoundedRectangle(bounds, 10);
        using SolidBrush fillBrush = new(fillColor);

        graphics.FillPath(fillBrush, path);
    }

    private void PaintElementContent(Graphics graphics, Rectangle bounds)
    {
        switch (State.Kind)
        {
            case WadevoDesignerElementKind.Artwork:
                PaintArtwork(graphics, bounds);
                break;

            case WadevoDesignerElementKind.Image:
                PaintImageFile(graphics, bounds);
                break;

            case WadevoDesignerElementKind.ProgressBar:
                PaintProgressBar(graphics, bounds);
                break;
        }
    }

    private void PaintImageFile(Graphics graphics, Rectangle bounds)
    {
        if (!string.IsNullOrWhiteSpace(State.ImagePath) &&
            ImageFileCache.TryGetValue(State.ImagePath, out Image? image))
        {
            Rectangle inner = Rectangle.Inflate(bounds, -4, -4);

            using GraphicsPath clipPath = CreateRoundedRectangle(inner, 8);
            using Region originalClip = graphics.Clip;

            graphics.SetClip(clipPath, CombineMode.Intersect);
            graphics.DrawImage(image, inner);
            graphics.Clip = originalClip;

            return;
        }

        PaintArtworkPlaceholder(graphics, bounds);
    }

    private void PaintArtwork(Graphics graphics, Rectangle bounds)
    {
        if (!string.IsNullOrWhiteSpace(State.ArtworkUrl) &&
            ArtworkCache.TryGetValue(State.ArtworkUrl, out Image? image))
        {
            Rectangle inner = Rectangle.Inflate(bounds, -4, -4);

            using GraphicsPath clipPath = CreateRoundedRectangle(inner, 8);
            using Region originalClip = graphics.Clip;

            graphics.SetClip(clipPath, CombineMode.Intersect);
            graphics.DrawImage(image, inner);
            graphics.Clip = originalClip;

            return;
        }

        PaintArtworkPlaceholder(graphics, bounds);
    }

    private void PaintElementBorder(Graphics graphics, Rectangle bounds)
    {
        Color borderColor = IsSelected
            ? WadevoDesignerColors.Selection
            : WadevoDesignerColors.ElementBorder;

        using GraphicsPath path = CreateRoundedRectangle(bounds, 10);
        using Pen borderPen = new(borderColor, IsSelected ? 2 : 1);

        graphics.DrawPath(borderPen, path);
    }

    private static void PaintArtworkPlaceholder(Graphics graphics, Rectangle bounds)
    {
        Rectangle inner = Rectangle.Inflate(bounds, -14, -14);

        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        using Pen linePen = new(WadevoDesignerColors.ElementBorder, 1);

        graphics.DrawLine(linePen, inner.Left, inner.Top, inner.Right, inner.Bottom);
        graphics.DrawLine(linePen, inner.Right, inner.Top, inner.Left, inner.Bottom);
    }

    private void PaintProgressBar(Graphics graphics, Rectangle bounds)
    {
        Rectangle inner = Rectangle.Inflate(bounds, -10, -6);

        if (inner.Width <= 0 || inner.Height <= 0)
        {
            return;
        }

        Color trackColor = Color.FromArgb(State.ProgressTrackColorArgb);
        Color fillColor = Color.FromArgb(State.ProgressFillColorArgb);

        using GraphicsPath trackPath = CreateRoundedRectangle(inner, Math.Max(4, inner.Height / 2));
        using SolidBrush trackBrush = new(trackColor);

        graphics.FillPath(trackBrush, trackPath);

        Rectangle fill = inner;
        fill.Width = Math.Max(8, inner.Width / 3);

        using GraphicsPath fillPath = CreateRoundedRectangle(fill, Math.Max(4, fill.Height / 2));
        using SolidBrush fillBrush = new(fillColor);

        graphics.FillPath(fillBrush, fillPath);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rectangle, int radius)
    {
        GraphicsPath path = new();

        int diameter = Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height));

        if (diameter <= 0)
        {
            path.AddRectangle(rectangle);
            return path;
        }

        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ownedFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}