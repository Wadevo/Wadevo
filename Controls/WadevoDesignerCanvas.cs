namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Wadevo.Core;

public sealed class WadevoDesignerCanvas : Panel
{
    private const int GridSize = 20;

    private readonly WadevoDesignerDocumentController _documentController = new();
    private readonly WadevoDesignerDragState _dragState = new();

    private WadevoDesignerElementControl? _resizingControl;
    private WadevoDesignerResizeHandle _resizeHandle;
    private Rectangle _resizeStartBounds;
    private Point _resizeStartMouse;

    private bool _isInitialized;

    // The canvas's own physical Size only ever grows beyond the viewport (to allow
    // scrolling when zoomed in past what fits) - it never shrinks below the viewport's
    // size, so the working area itself always stays comfortable to interact with. What
    // actually makes content appear smaller when zoomed out is _renderScaleX/Y below,
    // tracked separately precisely so it can keep shrinking even once physical Size has
    // already hit its floor at the viewport's own size.
    public float ZoomFactor { get; private set; } = 1.0f;

    private double _renderScaleX = 1.0;
    private double _renderScaleY = 1.0;
    private Panel? _hostViewport;

    // The actual multiplier currently used to convert a logical (true, unscaled) design
    // pixel into an on-screen pixel - combines "fit the design into the viewport" with
    // the user's own zoom choice. Everything that converts between screen and logical
    // coordinates (widget positioning, background rendering, drag/resize math) must go
    // through these, not ZoomFactor alone, since physical canvas Size and render scale
    // intentionally diverge once zoomed below what's needed to fill the viewport.
    public double RenderScaleX => _renderScaleX;
    public double RenderScaleY => _renderScaleY;

    // Called once by WadevoDesignerShell after adding this canvas to its scrollable
    // viewport - needed so zoom calculations have something to fit against and grow
    // beyond, instead of (wrongly) referencing the canvas's own size, which is what
    // this class is now computing, not reading.
    public void SetHostViewport(Panel viewport)
    {
        _hostViewport = viewport;
        viewport.Resize += (_, _) => ApplyZoomAndFit();
    }

    public void SetZoom(float zoom)
    {
        ZoomFactor = Math.Clamp(zoom, 0.25f, 2.0f);
        ApplyZoomAndFit();
    }

    private void ApplyZoomAndFit()
    {
        Size logicalSize = GetReferenceCanvasSize();

        int viewportWidth = _hostViewport?.ClientSize.Width ?? Width;
        int viewportHeight = _hostViewport?.ClientSize.Height ?? Height;

        // Fallback for the very first call during construction, before this control has
        // ever actually been sized by its parent yet.
        if (viewportWidth <= 0) viewportWidth = 900;
        if (viewportHeight <= 0) viewportHeight = 500;

        double baseFitScaleX = logicalSize.Width > 0 ? (double)viewportWidth / logicalSize.Width : 1.0;
        double baseFitScaleY = logicalSize.Height > 0 ? (double)viewportHeight / logicalSize.Height : 1.0;

        _renderScaleX = baseFitScaleX * ZoomFactor;
        _renderScaleY = baseFitScaleY * ZoomFactor;

        // Never smaller than the viewport (the working area stays comfortable even when
        // zoomed out - the shrinking happens via render scale above, leaving margin
        // around the smaller-rendered content instead of shrinking the canvas itself).
        // Only grows past the viewport once zoom pushes rendered content bigger than it.
        Width = Math.Max(viewportWidth, (int)(logicalSize.Width * _renderScaleX));
        Height = Math.Max(viewportHeight, (int)(logicalSize.Height * _renderScaleY));

        foreach (WadevoDesignerElementControl control in _documentController.Controls.Values)
        {
            control.RefreshFromState();
        }

        Invalidate();
    }

    private object? _theme;
    private string _previewTitle = "Song ID";
    private string _previewArtist = "Artist Name";
    private string _previewSong = "Song Title";
    private string _previewAlbum = "Album Name";
    private string _previewReleaseDate = "Release Date";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public WadevoDesignerDocument Document => _documentController.Document;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle PreviewBounds => ClientRectangle;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object? Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            RefreshPreview();
        }
    }

    public void SetPreviewText(string title, string artist, string song)
    {
        _previewTitle = title;
        _previewArtist = artist;
        _previewSong = song;

        RefreshPreview();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PreviewTitle
    {
        get => _previewTitle;
        set
        {
            _previewTitle = value;
            RefreshPreview();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PreviewArtist
    {
        get => _previewArtist;
        set
        {
            _previewArtist = value;
            RefreshPreview();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PreviewSong
    {
        get => _previewSong;
        set
        {
            _previewSong = value;
            RefreshPreview();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PreviewAlbum
    {
        get => _previewAlbum;
        set
        {
            _previewAlbum = value;
            RefreshPreview();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string PreviewReleaseDate
    {
        get => _previewReleaseDate;
        set
        {
            _previewReleaseDate = value;
            RefreshPreview();
        }
    }

    public WadevoDesignerCanvas()
    {
        DoubleBuffered = true;
        BackColor = WadevoTheme.Colors.Background;
        Dock = DockStyle.Fill;
        AutoScroll = false;
        TabStop = true;

        MouseDown += (_, _) => Focus();

        BuildDefaultDocument();
    }

    protected override bool IsInputKey(Keys keyData)
    {
        if (keyData == Keys.Delete)
        {
            return true;
        }

        return base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Delete)
        {
            DeleteSelectedCustomWidget();
        }
    }

    public void RefreshPreview()
    {
        if (!_isInitialized)
        {
            BuildDefaultDocument();
            return;
        }

        UpdateDefaultText("Artist", PreviewArtist);
        UpdateDefaultText("Song", PreviewSong);
        UpdateDefaultText("Album", PreviewAlbum);
        UpdateDefaultText("Release Date", PreviewReleaseDate);

        _documentController.Refresh(this);
        WireElementEvents();
        Invalidate();
    }

    public void SetArtworkUrl(string url)
    {
        WadevoDesignerElementState? element = Document.Elements
            .FirstOrDefault(item => item.Name == "Album Artwork");

        if (element is null)
        {
            return;
        }

        element.ArtworkUrl = url;

        _documentController.Refresh(this);
        WireElementEvents();
        Invalidate();
    }

    public IReadOnlyList<WadevoDesignerElementState> GetElementsSnapshot()
    {
        return Document.Elements.ToList();
    }

    public void LoadElements(IEnumerable<WadevoDesignerElementState> elements)
    {
        _documentController.Clear(this);

        foreach (WadevoDesignerElementState element in elements)
        {
            _documentController.Add(
                new WadevoDesignerElementState
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = element.Name,
                    Kind = element.Kind,
                    X = element.X,
                    Y = element.Y,
                    Width = element.Width,
                    Height = element.Height,
                    Text = element.Text,
                    FontFamily = element.FontFamily,
                    FontSize = element.FontSize,
                    FontBold = element.FontBold,
                    FontColorArgb = element.FontColorArgb,
                    ArtworkUrl = element.ArtworkUrl,
                    ImagePath = element.ImagePath,
                    CountdownTargetUtc = element.CountdownTargetUtc,
                    CountdownLabel = element.CountdownLabel,
                    CountdownCompletedText = element.CountdownCompletedText,
                    ClockFormat = element.ClockFormat,
                    SongQueueMaxVisible = element.SongQueueMaxVisible,
                    GoalMetric = element.GoalMetric,
                    GoalPlatform = element.GoalPlatform,
                    GoalTarget = element.GoalTarget,
                    ProgressFillColorArgb = element.ProgressFillColorArgb,
                    ProgressTrackColorArgb = element.ProgressTrackColorArgb,
                    IsVisible = element.IsVisible,
                    IsLocked = element.IsLocked
                },
                this);
        }

        _isInitialized = true;

        WireElementEvents();
        SetZoom(ZoomFactor);
        Invalidate();
    }

    // Used when starting a blank "Custom" overlay instead of a "Song ID" one - skips
    // the auto-seeded Song ID/Artist/Song/etc fields entirely rather than requiring
    // someone to delete six fields they never wanted in the first place.
    public void ClearAllElements()
    {
        _documentController.Clear(this);
        _isInitialized = true;

        WireElementEvents();
        Invalidate();
    }

    private void BuildDefaultDocument()
    {
        if (_isInitialized)
        {
            return;
        }

        _documentController.Clear(this);

        AddDefaultElement("Artist", WadevoDesignerElementKind.Text, PreviewArtist, 80, 70, 360, 38);
        AddDefaultElement("Song", WadevoDesignerElementKind.Text, PreviewSong, 80, 115, 420, 38);
        AddDefaultElement("Album", WadevoDesignerElementKind.Text, PreviewAlbum, 80, 160, 360, 32);
        AddDefaultElement("Release Date", WadevoDesignerElementKind.Text, PreviewReleaseDate, 80, 200, 260, 32);
        AddDefaultElement("Album Artwork", WadevoDesignerElementKind.Artwork, "", 530, 70, 150, 150);
        // Note: no default progress bar here. Wadevo can't read real playback position from
        // Serato today, so a bar that always shows the same fixed fill would be misleading.
        // The ProgressBar element type is still available for anyone who wants a purely
        // decorative bar in their design; it just isn't added automatically anymore.

        _isInitialized = true;

        WireElementEvents();
        Invalidate();
    }

    private void AddDefaultElement(
        string name,
        WadevoDesignerElementKind kind,
        string text,
        int x,
        int y,
        int width,
        int height)
    {
        _documentController.Add(
            new WadevoDesignerElementState
            {
                Id = Guid.NewGuid().ToString("N"),
                Kind = kind,
                Name = name,
                Text = text,
                X = x,
                Y = y,
                Width = width,
                Height = height
            },
            this);
    }

    private void UpdateDefaultText(string name, string text)
    {
        WadevoDesignerElementState? element = Document.Elements
            .FirstOrDefault(item => item.Name == name);

        if (element is not null)
        {
            element.Text = text;
        }
    }

    private ContextMenuStrip BuildElementContextMenu()
    {
        ContextMenuStrip menu = new();

        ToolStripMenuItem deleteItem = new("🗑 Delete Widget");
        deleteItem.Click += (_, _) => DeleteSelectedCustomWidget();

        menu.Items.Add(deleteItem);

        return menu;
    }

    private void WireElementEvents()
    {
        foreach (WadevoDesignerElementControl control in _documentController.Controls.Values)
        {
            control.MouseDown -= ElementControl_MouseDown;
            control.MouseMove -= ElementControl_MouseMove;
            control.MouseUp -= ElementControl_MouseUp;
            control.MouseDoubleClick -= ElementControl_MouseDoubleClick;

            control.MouseDown += ElementControl_MouseDown;
            control.MouseMove += ElementControl_MouseMove;
            control.MouseUp += ElementControl_MouseUp;
            control.MouseDoubleClick += ElementControl_MouseDoubleClick;

            // A right-click menu doesn't depend on the canvas having keyboard focus the
            // way the Delete key does - deleting only ever worked via that key press,
            // which made it fragile for any element where focus doesn't land on the
            // canvas as expected after clicking it.
            control.ContextMenuStrip = BuildElementContextMenu();

            foreach (Control child in control.Controls)
            {
                child.MouseDown -= ElementChild_MouseDown;
                child.MouseMove -= ElementChild_MouseMove;
                child.MouseUp -= ElementChild_MouseUp;
                child.MouseDoubleClick -= ElementChild_MouseDoubleClick;

                child.MouseDown += ElementChild_MouseDown;
                child.MouseMove += ElementChild_MouseMove;
                child.MouseUp += ElementChild_MouseUp;
                child.MouseDoubleClick += ElementChild_MouseDoubleClick;

                child.ContextMenuStrip = control.ContextMenuStrip;
            }
        }
    }

    private void ElementControl_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (sender is WadevoDesignerElementControl control)
        {
            TryEditElementStyle(control);
        }
    }

    private void ElementChild_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (sender is Control child && child.Parent is WadevoDesignerElementControl control)
        {
            TryEditElementStyle(control);
        }
    }

    // The original Song ID fields can have their style edited here too, just not their
    // text content - that's still managed through the Text dropdown, since it also needs to
    // update the shared preview settings that dropdown reads from.
    private static readonly HashSet<string> ProtectedElementNames = new()
    {
        "Song ID Label", "Artist", "Song", "Album", "Release Date", "Album Artwork"
    };

    private void TryEditElementStyle(WadevoDesignerElementControl control)
    {
        if (control.State.Kind == WadevoDesignerElementKind.Countdown)
        {
            EditCountdownSettings(control);
            return;
        }

        if (control.State.Kind == WadevoDesignerElementKind.SongQueue)
        {
            EditSongQueueSettings(control);
            return;
        }

        if (control.State.Kind == WadevoDesignerElementKind.ProgressBar)
        {
            EditGoalBarSettings(control);
            return;
        }

        bool isStylableKind = control.State.Kind is
            WadevoDesignerElementKind.Text or
            WadevoDesignerElementKind.Clock or
            WadevoDesignerElementKind.VoteTally;

        if (!isStylableKind)
        {
            return;
        }

        bool hasNoTextField = control.State.Kind is
            WadevoDesignerElementKind.Clock;

        bool isProtected = ProtectedElementNames.Contains(control.State.Name) || hasNoTextField;

        using WadevoTextStyleForm styleForm = new(
            isProtected ? $"Style: {control.State.Name}" : "Edit Text Widget",
            showTextField: !isProtected,
            control.State.Text,
            control.State.FontFamily,
            control.State.FontSize,
            control.State.FontBold,
            Color.FromArgb(control.State.FontColorArgb));

        if (styleForm.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        if (!isProtected)
        {
            control.State.Text = styleForm.TextValue;
        }

        control.State.FontFamily = styleForm.SelectedFont;
        control.State.FontSize = styleForm.SelectedSize;
        control.State.FontBold = styleForm.SelectedBold;
        control.State.FontColorArgb = styleForm.SelectedColor.ToArgb();

        control.RefreshFromState();

        _documentController.Save();
    }

    // Countdown widgets previously had no way to change their target date/time, label, or
    // completed message at all after creation - only font styling was reachable, and even
    // that skipped the actual countdown-specific settings entirely.
    private void EditCountdownSettings(WadevoDesignerElementControl control)
    {
        using WadevoCountdownEditForm form = new(
            control.State.CountdownTargetUtc,
            control.State.CountdownLabel,
            control.State.CountdownCompletedText,
            control.State.FontFamily,
            control.State.FontSize,
            control.State.FontBold,
            Color.FromArgb(control.State.FontColorArgb));

        if (form.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        control.State.CountdownTargetUtc = form.SelectedTargetUtc;
        control.State.CountdownLabel = form.SelectedLabel;
        control.State.CountdownCompletedText = form.SelectedCompletedText;
        control.State.FontFamily = form.SelectedFont;
        control.State.FontSize = form.SelectedSize;
        control.State.FontBold = form.SelectedBold;
        control.State.FontColorArgb = form.SelectedColor.ToArgb();

        control.RefreshFromState();

        _documentController.Save();
    }

    private void EditSongQueueSettings(WadevoDesignerElementControl control)
    {
        using WadevoSongQueueEditForm form = new(
            control.State.FontFamily,
            control.State.FontSize,
            control.State.FontBold,
            Color.FromArgb(control.State.FontColorArgb),
            control.State.SongQueueMaxVisible);

        if (form.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        control.State.FontFamily = form.SelectedFont;
        control.State.FontSize = form.SelectedSize;
        control.State.FontBold = form.SelectedBold;
        control.State.FontColorArgb = form.SelectedColor.ToArgb();
        control.State.SongQueueMaxVisible = form.SelectedMaxVisible;

        control.RefreshFromState();

        _documentController.Save();
    }

    private void EditGoalBarSettings(WadevoDesignerElementControl control)
    {
        using WadevoGoalEditForm form = new(
            control.State.GoalMetric,
            control.State.GoalPlatform,
            control.State.GoalTarget,
            Color.FromArgb(control.State.ProgressFillColorArgb),
            Color.FromArgb(control.State.ProgressTrackColorArgb),
            control.State.FontFamily,
            control.State.FontSize,
            control.State.FontBold,
            Color.FromArgb(control.State.FontColorArgb));

        if (form.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        control.State.GoalMetric = form.SelectedMetric;
        control.State.GoalPlatform = form.SelectedPlatform;
        control.State.GoalTarget = form.SelectedTarget;
        control.State.ProgressFillColorArgb = form.SelectedFillColor.ToArgb();
        control.State.ProgressTrackColorArgb = form.SelectedTrackColor.ToArgb();
        control.State.FontFamily = form.SelectedFont;
        control.State.FontSize = form.SelectedSize;
        control.State.FontBold = form.SelectedBold;
        control.State.FontColorArgb = form.SelectedTextColor.ToArgb();

        control.RefreshFromState();

        _documentController.Save();
    }

    public void AddTextWidget()
    {
        int widgetNumber = 1;

        while (Document.Elements.Any(item => item.Name == $"Custom Text {widgetNumber}"))
        {
            widgetNumber++;
        }

        WadevoDesignerElementState element = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Custom Text {widgetNumber}",
            Kind = WadevoDesignerElementKind.Text,
            Text = "New Text",
            Bounds = new Rectangle(80, 40, 260, 42)
        };

        _documentController.Add(element, this);
        WireElementEvents();
        Invalidate();
    }

    public void AddChatFeedWidget()
    {
        int widgetNumber = 1;

        while (Document.Elements.Any(item => item.Name == $"Chat Feed {widgetNumber}"))
        {
            widgetNumber++;
        }

        WadevoDesignerElementState element = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Chat Feed {widgetNumber}",
            Kind = WadevoDesignerElementKind.ChatFeed,
            Bounds = new Rectangle(80, 40, 380, 320)
        };

        _documentController.Add(element, this);
        WireElementEvents();
        Invalidate();
    }

    public void AddImageWidget(string imagePath)
    {
        int widgetNumber = 1;

        while (Document.Elements.Any(item => item.Name == $"Image {widgetNumber}"))
        {
            widgetNumber++;
        }

        WadevoDesignerElementState element = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Image {widgetNumber}",
            Kind = WadevoDesignerElementKind.Image,
            ImagePath = imagePath,
            Bounds = new Rectangle(80, 40, 160, 160)
        };

        _documentController.Add(element, this);
        WireElementEvents();
        Invalidate();
    }

    public void AddClockWidget()
    {
        int widgetNumber = 1;

        while (Document.Elements.Any(item => item.Name == $"Clock {widgetNumber}"))
        {
            widgetNumber++;
        }

        WadevoDesignerElementState element = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Clock {widgetNumber}",
            Kind = WadevoDesignerElementKind.Clock,
            ClockFormat = "h:mm tt",
            Bounds = new Rectangle(80, 40, 220, 50)
        };

        _documentController.Add(element, this);
        WireElementEvents();
        Invalidate();
    }

    public void AddCountdownWidget()
    {
        int widgetNumber = 1;

        while (Document.Elements.Any(item => item.Name == $"Countdown {widgetNumber}"))
        {
            widgetNumber++;
        }

        WadevoDesignerElementState element = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Countdown {widgetNumber}",
            Kind = WadevoDesignerElementKind.Countdown,
            CountdownTargetUtc = DateTime.UtcNow.AddHours(1),
            Bounds = new Rectangle(80, 40, 260, 50)
        };

        _documentController.Add(element, this);
        WireElementEvents();
        Invalidate();
    }

    public void AddGoalBarWidget()
    {
        int widgetNumber = 1;

        while (Document.Elements.Any(item => item.Name == $"Goal Bar {widgetNumber}"))
        {
            widgetNumber++;
        }

        WadevoDesignerElementState element = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Goal Bar {widgetNumber}",
            Kind = WadevoDesignerElementKind.ProgressBar,
            GoalMetric = "Followers",
            GoalTarget = 100,
            Bounds = new Rectangle(80, 40, 360, 60)
        };

        _documentController.Add(element, this);
        WireElementEvents();
        Invalidate();
    }

    public void AddVoteTallyWidget()
    {
        int widgetNumber = 1;

        while (Document.Elements.Any(item => item.Name == $"Vote Tally {widgetNumber}"))
        {
            widgetNumber++;
        }

        WadevoDesignerElementState element = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Vote Tally {widgetNumber}",
            Kind = WadevoDesignerElementKind.VoteTally,
            Text = "🗳️ {count} votes",
            Bounds = new Rectangle(80, 40, 260, 60)
        };

        _documentController.Add(element, this);
        WireElementEvents();
        Invalidate();
    }

    public void AddSongQueueWidget()
    {
        int widgetNumber = 1;

        while (Document.Elements.Any(item => item.Name == $"Song Queue {widgetNumber}"))
        {
            widgetNumber++;
        }

        WadevoDesignerElementState element = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"Song Queue {widgetNumber}",
            Kind = WadevoDesignerElementKind.SongQueue,
            SongQueueMaxVisible = 5,
            Bounds = new Rectangle(80, 40, 320, 220)
        };

        _documentController.Add(element, this);
        WireElementEvents();
        Invalidate();
    }

    public bool DeleteSelectedCustomWidget()
    {
        WadevoDesignerElementState? selected = _documentController.SelectedElements.FirstOrDefault();

        // These used to be permanently undeletable - if someone didn't want to show
        // "Release Date" or "Album" at all, there was no way to get rid of it, only to
        // leave it sitting there unused. RefreshPreview/SetArtworkUrl already look these
        // up by name and safely no-op if they're missing, so deleting one is safe -
        // it just means that piece of live data won't render, same as any other widget.
        if (selected is null)
        {
            return false;
        }

        _documentController.Remove(selected.Id, this);
        Invalidate();

        return true;
    }

    private void ElementControl_MouseDown(object? sender, MouseEventArgs e)
    {
        if (sender is WadevoDesignerElementControl control)
        {
            HandlePointerDown(control, PointToClient(control.PointToScreen(e.Location)));
        }
    }

    private void ElementChild_MouseDown(object? sender, MouseEventArgs e)
    {
        if (sender is Control child && child.Parent is WadevoDesignerElementControl control)
        {
            HandlePointerDown(control, PointToClient(child.PointToScreen(e.Location)));
        }
    }

    private void HandlePointerDown(WadevoDesignerElementControl control, Point canvasMouseLocation)
    {
        Focus();

        Point controlLocalPoint = new(canvasMouseLocation.X - control.Left, canvasMouseLocation.Y - control.Top);
        WadevoDesignerResizeHandle handle = control.HitTestHandle(controlLocalPoint);

        if (handle != WadevoDesignerResizeHandle.None)
        {
            BeginElementResize(control, handle, canvasMouseLocation);
            return;
        }

        BeginElementDrag(control, canvasMouseLocation);
    }

    private void BeginElementResize(WadevoDesignerElementControl control, WadevoDesignerResizeHandle handle, Point canvasMouseLocation)
    {
        // BeginElementDrag below calls Select(), but this resize path previously didn't -
        // meaning a click that happened to land on a resize handle (easy to do on a large
        // widget like an image) would resize it without ever registering it as selected in
        // the document controller. Pressing Delete right after would then silently find
        // nothing selected and do nothing, with no visible error.
        _documentController.Select(control.State.Id);

        _resizingControl = control;
        _resizeHandle = handle;
        _resizeStartBounds = control.State.Bounds;
        _resizeStartMouse = canvasMouseLocation;
        control.Capture = true;
    }

    private WadevoDesignerElementControl? _pendingDragControl;

    private void BeginElementDrag(WadevoDesignerElementControl control, Point canvasMouseLocation)
    {
        _documentController.Select(control.State.Id);
        _dragState.Begin(canvasMouseLocation, _documentController.SelectedElements);
        _pendingDragControl = control;
        // Deliberately not capturing the mouse yet - see MoveSelectedElements for why.
    }

    private void ElementControl_MouseMove(object? sender, MouseEventArgs e)
    {
        if (sender is WadevoDesignerElementControl control)
        {
            HandlePointerMove(PointToClient(control.PointToScreen(e.Location)));
        }
    }

    private void ElementChild_MouseMove(object? sender, MouseEventArgs e)
    {
        if (sender is Control child)
        {
            HandlePointerMove(PointToClient(child.PointToScreen(e.Location)));
        }
    }

    private void HandlePointerMove(Point canvasMouseLocation)
    {
        if (_resizingControl is not null)
        {
            ResizeElement(canvasMouseLocation);
        }
        else
        {
            MoveSelectedElements(canvasMouseLocation);
        }
    }

    private void ResizeElement(Point canvasMouseLocation)
    {
        if (_resizingControl is null)
        {
            return;
        }

        int deltaX = (int)((canvasMouseLocation.X - _resizeStartMouse.X) / RenderScaleX);
        int deltaY = (int)((canvasMouseLocation.Y - _resizeStartMouse.Y) / RenderScaleY);

        Rectangle bounds = _resizeStartBounds;

        switch (_resizeHandle)
        {
            case WadevoDesignerResizeHandle.BottomRight:
                bounds.Width += deltaX;
                bounds.Height += deltaY;
                break;

            case WadevoDesignerResizeHandle.BottomLeft:
                bounds.X += deltaX;
                bounds.Width -= deltaX;
                bounds.Height += deltaY;
                break;

            case WadevoDesignerResizeHandle.TopRight:
                bounds.Y += deltaY;
                bounds.Width += deltaX;
                bounds.Height -= deltaY;
                break;

            case WadevoDesignerResizeHandle.TopLeft:
                bounds.X += deltaX;
                bounds.Y += deltaY;
                bounds.Width -= deltaX;
                bounds.Height -= deltaY;
                break;
        }

        const int minSize = 30;

        bounds.Width = Math.Max(minSize, bounds.Width);
        bounds.Height = Math.Max(minSize, bounds.Height);

        _resizingControl.State.Bounds = new Rectangle(
            SnapToGrid(bounds.X),
            SnapToGrid(bounds.Y),
            SnapToGrid(bounds.Width),
            SnapToGrid(bounds.Height));

        _resizingControl.RefreshFromState();
    }

    private void MoveSelectedElements(Point canvasMouseLocation)
    {
        if (!_dragState.IsDragging)
        {
            return;
        }

        if (_pendingDragControl is not null)
        {
            int distanceX = Math.Abs(canvasMouseLocation.X - _dragState.StartMouse.X);
            int distanceY = Math.Abs(canvasMouseLocation.Y - _dragState.StartMouse.Y);

            if (distanceX < SystemInformation.DragSize.Width && distanceY < SystemInformation.DragSize.Height)
            {
                // Still within click tolerance - this might end up being a single click or a
                // double-click, not a drag. Don't capture the mouse or move anything yet.
                return;
            }

            _pendingDragControl.Capture = true;
            _pendingDragControl = null;
        }

        foreach (WadevoDesignerElementState element in _documentController.SelectedElements)
        {
            Rectangle movedBounds = _dragState.GetMovedBounds(element, canvasMouseLocation, RenderScaleX, RenderScaleY);

            element.Bounds = new Rectangle(
                SnapToGrid(movedBounds.X),
                SnapToGrid(movedBounds.Y),
                movedBounds.Width,
                movedBounds.Height);

            if (_documentController.Controls.TryGetValue(element.Id, out WadevoDesignerElementControl? control))
            {
                control.RefreshFromState();
            }
        }
    }

    private void ElementControl_MouseUp(object? sender, MouseEventArgs e)
    {
        EndDrag();
    }

    private void ElementChild_MouseUp(object? sender, MouseEventArgs e)
    {
        EndDrag();
    }

    private void EndDrag()
    {
        bool wasDragging = _dragState.IsDragging;
        bool wasResizing = _resizingControl is not null;

        _pendingDragControl = null;

        if (!wasDragging && !wasResizing)
        {
            return;
        }

        _dragState.End();
        _resizingControl = null;

        _documentController.Save();

        foreach (WadevoDesignerElementControl control in _documentController.Controls.Values)
        {
            control.Capture = false;
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        _dragState.End();
        _documentController.ClearSelection();

        if (_backgroundImage is not null && e.Button == MouseButtons.Left &&
            TryGetBackgroundCornerAt(e.Location, out Point corner))
        {
            _isResizingBackground = true;
            _backgroundResizeCorner = corner;
            _backgroundResizeStartMouse = e.Location;
            _backgroundResizeStartWidth = BackgroundWidthPercent;
            _backgroundResizeStartHeight = BackgroundHeightPercent;

            Rectangle currentRect = GetBackgroundDisplayRect();
            _backgroundResizeStartCenter = new Point(
                currentRect.Left + currentRect.Width / 2,
                currentRect.Top + currentRect.Height / 2);

            Cursor = Cursors.SizeNWSE;
            return;
        }

        // Empty canvas area previously did nothing on mousedown besides clearing
        // selection - there was no way at all to reposition the background image once
        // it was set, only scale mode/zoom. A plain click-drag on empty canvas now pans it.
        if (_backgroundImage is not null && e.Button == MouseButtons.Left)
        {
            _isDraggingBackground = true;
            _backgroundDragStartMouse = e.Location;
            _backgroundDragStartOffsetX = BackgroundOffsetX;
            _backgroundDragStartOffsetY = BackgroundOffsetY;
            Cursor = Cursors.SizeAll;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isResizingBackground)
        {
            // Horizontal and vertical distance from center computed separately, each
            // driving its own width/height percentage - this is the actual fix for
            // "can't make the image skinnier or wider on its own," since the old version
            // combined both directions into one uniform zoom ratio.
            Point center = _backgroundResizeStartCenter;

            double startDistanceX = Math.Abs(_backgroundResizeStartMouse.X - center.X);
            double startDistanceY = Math.Abs(_backgroundResizeStartMouse.Y - center.Y);
            double currentDistanceX = Math.Abs(e.X - center.X);
            double currentDistanceY = Math.Abs(e.Y - center.Y);

            if (startDistanceX < 1) startDistanceX = 1;
            if (startDistanceY < 1) startDistanceY = 1;

            double ratioX = currentDistanceX / startDistanceX;
            double ratioY = currentDistanceY / startDistanceY;

            int newWidth = (int)Math.Round(_backgroundResizeStartWidth * ratioX);
            int newHeight = (int)Math.Round(_backgroundResizeStartHeight * ratioY);

            BackgroundWidthPercent = Math.Clamp(newWidth, 20, 400);
            BackgroundHeightPercent = Math.Clamp(newHeight, 20, 400);

            Invalidate();
            return;
        }

        if (!_isDraggingBackground)
        {
            return;
        }

        // Mouse movement is measured in this control's own on-screen pixels, which vary
        // with window size - converting to reference-space pixels here keeps the stored
        // offset consistent with the exported overlay's actual pixel space, the same fix
        // applied to sizing above.
        double scaleX = _renderScaleX > 0 ? 1.0 / _renderScaleX : 1.0;
        double scaleY = _renderScaleY > 0 ? 1.0 / _renderScaleY : 1.0;

        int deltaX = (int)((e.X - _backgroundDragStartMouse.X) * scaleX);
        int deltaY = (int)((e.Y - _backgroundDragStartMouse.Y) * scaleY);

        BackgroundOffsetX = _backgroundDragStartOffsetX + deltaX;
        BackgroundOffsetY = _backgroundDragStartOffsetY + deltaY;

        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        EndDrag();

        if (_isResizingBackground)
        {
            _isResizingBackground = false;
            Cursor = Cursors.Default;
            _documentController.Save();
        }

        if (_isDraggingBackground)
        {
            _isDraggingBackground = false;
            Cursor = Cursors.Default;
        }
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

    private static int SnapToGrid(int value)
    {
        return (int)Math.Round(value / (double)GridSize) * GridSize;
    }

    private Image? _backgroundImage;

    public string? BackgroundImagePath { get; private set; }

    public WadevoBackgroundScaleMode BackgroundScaleMode { get; set; } = WadevoBackgroundScaleMode.Fill;

    public bool BackgroundRoundedCorners { get; set; } = true;

    // Independent from each other (unlike the old single Zoom% this replaces) - dragging
    // a corner now stretches width and height separately based on horizontal vs vertical
    // mouse movement, the same way resizing any other widget on the canvas works, rather
    // than a uniform zoom that couldn't make an image "skinnier" or "wider" on its own.
    public int BackgroundWidthPercent { get; set; } = 100;

    public int BackgroundHeightPercent { get; set; } = 100;

    public int BackgroundOpacityPercent { get; set; } = 100;

    public int BackgroundOffsetX { get; set; }

    public int BackgroundOffsetY { get; set; }

    private bool _isDraggingBackground;
    private Point _backgroundDragStartMouse;
    private int _backgroundDragStartOffsetX;
    private int _backgroundDragStartOffsetY;

    private bool _isResizingBackground;
    private Point _backgroundResizeCorner;
    private Point _backgroundResizeStartMouse;
    private int _backgroundResizeStartWidth;
    private int _backgroundResizeStartHeight;
    private Point _backgroundResizeStartCenter;

    public void SetBackgroundOffset(int offsetX, int offsetY)
    {
        BackgroundOffsetX = offsetX;
        BackgroundOffsetY = offsetY;
        Invalidate();
    }

    public string AnimationType { get; set; } = "None";

    public int AnimationDurationMs { get; set; } = 500;

    // Separate from AnimationDurationMs (which is the slide transition's own speed,
    // capped at a few seconds) - this is how long the overlay actually stays fully
    // visible before it animates back out. 0 means it just stays up once shown, same as
    // AlwaysOn's behavior.
    public int AutoHideSeconds { get; set; } = 0;

    public bool AlwaysOn { get; set; } = false;

    public void SetBackgroundImage(string? path)
    {
        _backgroundImage?.Dispose();
        _backgroundImage = null;
        BackgroundImagePath = null;

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
                _backgroundImage = Image.FromStream(stream);
                BackgroundImagePath = path;
            }
            catch
            {
                _backgroundImage = null;
                BackgroundImagePath = null;
            }
        }

        // A hover cursor is the only hint that the background can be dragged at all -
        // there's no other visual affordance for it, so this at least tells people it's
        // possible before they've clicked anything.
        Cursor = _backgroundImage is not null ? Cursors.SizeAll : Cursors.Default;

        Invalidate();
    }

    public void SetBackgroundStyle(WadevoBackgroundScaleMode scaleMode, bool roundedCorners, int widthPercent = 100, int heightPercent = 100, int opacityPercent = 100)
    {
        BackgroundScaleMode = scaleMode;
        BackgroundRoundedCorners = roundedCorners;
        BackgroundWidthPercent = widthPercent;
        BackgroundHeightPercent = heightPercent;
        BackgroundOpacityPercent = opacityPercent;
        Invalidate();
    }

    // The Designer's own on-screen size is just whatever the current app window/viewport
    // happens to be - it has no relationship to the exported overlay's actual dimensions,
    // which OverlayServer.cs computes purely from the elements' own bounds. Percentage-based
    // background sizing needs to be relative to THIS reference frame, not the arbitrary
    // control size, or the same percentage looks completely different between the Designer
    // preview and the real thing in OBS. Must match OverlayServer.cs's own formula exactly.
    private Size GetReferenceCanvasSize()
    {
        var elements = Document.Elements;

        int width = elements.Count == 0
            ? 900
            : Math.Max(900, elements.Max(e => e.X + e.Width) + 40);

        int height = elements.Count == 0
            ? 400
            : Math.Max(400, elements.Max(e => e.Y + e.Height) + 40);

        return new Size(Math.Max(320, width), Math.Max(160, height));
    }

    private void PaintBackgroundImage(Graphics graphics)
    {
        if (_backgroundImage is null)
        {
            return;
        }

        Rectangle destRect = GetBackgroundDisplayRect();
        Rectangle canvasRect = new(0, 0, Width, Height);

        GraphicsPath? clipPath = null;

        if (BackgroundRoundedCorners)
        {
            clipPath = CreateRoundedRectangle(canvasRect, 16);
            graphics.SetClip(clipPath);
        }

        if (BackgroundOpacityPercent >= 100)
        {
            graphics.DrawImage(_backgroundImage, destRect);
        }
        else
        {
            // DrawImage has no opacity parameter of its own - scaling the alpha channel via
            // a ColorMatrix through ImageAttributes is the standard GDI+ way to fade an
            // image without pre-baking transparency into the source file itself.
            float alpha = Math.Clamp(BackgroundOpacityPercent, 0, 100) / 100f;

            using ImageAttributes attributes = new();

            ColorMatrix matrix = new()
            {
                Matrix33 = alpha
            };

            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            graphics.DrawImage(
                _backgroundImage,
                destRect,
                0,
                0,
                _backgroundImage.Width,
                _backgroundImage.Height,
                GraphicsUnit.Pixel,
                attributes);
        }

        if (clipPath is not null)
        {
            graphics.ResetClip();
            clipPath.Dispose();
        }
    }

    private Rectangle ComputeBackgroundDestRect(Rectangle canvasRect)
    {
        if (_backgroundImage is null)
        {
            return canvasRect;
        }

        (int x, int y, int width, int height) = Wadevo.Services.BackgroundRectCalculator.ComputeDestRect(
            canvasRect.X, canvasRect.Y, canvasRect.Width, canvasRect.Height,
            _backgroundImage.Width, _backgroundImage.Height,
            BackgroundScaleMode.ToString(), BackgroundWidthPercent, BackgroundHeightPercent,
            BackgroundOffsetX, BackgroundOffsetY);

        return new Rectangle(x, y, width, height);
    }

    

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        PaintBackgroundImage(e.Graphics);

        using Pen gridPen = new(Color.FromArgb(22, WadevoTheme.Colors.Accent), 1);

        for (int x = 0; x < Width; x += GridSize)
        {
            e.Graphics.DrawLine(gridPen, x, 0, x, Height);
        }

        for (int y = 0; y < Height; y += GridSize)
        {
            e.Graphics.DrawLine(gridPen, 0, y, Width, y);
        }

        if (_backgroundImage is not null)
        {
            PaintBackgroundCornerHandles(e.Graphics);
        }
    }

    private void PaintBackgroundCornerHandles(Graphics graphics)
    {
        using SolidBrush handleBrush = new(WadevoTheme.Colors.Accent);
        using Pen handleOutline = new(Color.White, 1.5f);

        foreach (Point corner in GetBackgroundCornerPoints())
        {
            Rectangle handleRect = new(
                corner.X - BackgroundHandleSize / 2,
                corner.Y - BackgroundHandleSize / 2,
                BackgroundHandleSize,
                BackgroundHandleSize);

            graphics.FillRectangle(handleBrush, handleRect);
            graphics.DrawRectangle(handleOutline, handleRect);
        }
    }

    // The background always covers the full canvas rather than having its own independent
    // Previously always the canvas's own outer corners, regardless of the background's
    // actual current size/position - once scaled down or panned, the interactive handles
    // no longer lined up with the image you could actually see, unlike every other widget
    // where handles sit right on its own visible border. Using the same on-screen rect
    // PaintBackgroundImage draws from fixes that mismatch.
    private const int BackgroundHandleSize = 12;
    private const int BackgroundHandleHitRadius = 14;

    private IEnumerable<Point> GetBackgroundCornerPoints()
    {
        Rectangle rect = GetBackgroundDisplayRect();

        yield return new Point(rect.Left, rect.Top);
        yield return new Point(rect.Right, rect.Top);
        yield return new Point(rect.Left, rect.Bottom);
        yield return new Point(rect.Right, rect.Bottom);
    }

    // The exact same on-screen rectangle PaintBackgroundImage computes and draws into -
    // kept as its own method so hit-testing and resize math can both reference the same
    // real, current position instead of duplicating (and risking drifting from) that math.
    private Rectangle GetBackgroundDisplayRect()
    {
        if (_backgroundImage is null)
        {
            return new Rectangle(0, 0, Width, Height);
        }

        Size referenceCanvasSize = GetReferenceCanvasSize();
        Rectangle referenceRect = new(0, 0, referenceCanvasSize.Width, referenceCanvasSize.Height);
        Rectangle destRect = ComputeBackgroundDestRect(referenceRect);

        return new Rectangle(
            (int)(destRect.X * _renderScaleX),
            (int)(destRect.Y * _renderScaleY),
            (int)(destRect.Width * _renderScaleX),
            (int)(destRect.Height * _renderScaleY));
    }

    private bool TryGetBackgroundCornerAt(Point location, out Point corner)
    {
        foreach (Point candidate in GetBackgroundCornerPoints())
        {
            int dx = location.X - candidate.X;
            int dy = location.Y - candidate.Y;

            if (dx * dx + dy * dy <= BackgroundHandleHitRadius * BackgroundHandleHitRadius)
            {
                corner = candidate;
                return true;
            }
        }

        corner = Point.Empty;
        return false;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (_isInitialized)
        {
            _documentController.Refresh(this);
            WireElementEvents();
        }
    }
}