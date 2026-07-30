namespace Wadevo;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Blaze;

public partial class MainForm : Form
{
    private const int RefreshSeconds = 3;

    // Win32 message/hit-test constants used below to make this borderless window support
    // native Aero Snap (drag-to-top-edge to maximize, drag-to-side to half-screen, corner
    // drag to quarter-screen) plus edge-drag resizing - none of which work automatically
    // on a FormBorderStyle.None window, since that strips the WS_THICKFRAME/WS_CAPTION
    // style bits Windows' DWM checks before offering Snap at all.
    private const int WM_NCCALCSIZE = 0x0083;
    private const int WM_NCHITTEST = 0x0084;

    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    private const int ResizeBorderThickness = 8;

    private SeratoService? _seratoService;
    private SeratoHistoryReader? _seratoLocalReader;
    private VirtualDjNowPlayingReader? _virtualDjReader;
    private OverlayServer? _overlayServer;
    private CancellationTokenSource? _songReaderCancellation;

    private readonly WadevoWindowFrame _frame;
    private readonly WadevoSplash _splash;
    private WadevoShell? _shell;

    private FormWindowState _previousWindowState;
    private Rectangle _previousBounds;
    private bool _isFullscreen;

    private string _lastSong = "";
    private string _currentSong = "Artist - Song Title";
    private MusicMetadataModel? _currentMetadata;

    public MainForm()
    {
        InitializeComponent();

        Text = "Wadevo";
        FormBorderStyle = FormBorderStyle.None;
        MinimumSize = new Size(1320, 780);
        Size = new Size(1400, 850);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;
        KeyPreview = true;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        _frame = new WadevoWindowFrame();
        _splash = new WadevoSplash();

        Controls.Add(_frame);
        _frame.ContentHost.Controls.Add(_splash);

        BeginStartup();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;

            // WS_CAPTION + WS_THICKFRAME are what make Windows treat this as a normal,
            // resizable top-level window eligible for Snap. WM_NCCALCSIZE below then
            // reclaims the entire window as client area, so none of the native
            // titlebar/border actually gets drawn - WadevoWindowFrame's custom titlebar
            // is the only thing visible, same as before.
            cp.Style |= 0x00C00000; // WS_CAPTION
            cp.Style |= 0x00040000; // WS_THICKFRAME
            cp.Style |= 0x00020000; // WS_MINIMIZEBOX
            cp.Style |= 0x00010000; // WS_MAXIMIZEBOX

            return cp;
        }
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_NCCALCSIZE when m.WParam != IntPtr.Zero:
                // Accepting Windows' proposed rectangle as-is (rather than the default
                // handling, which would carve out space for a native titlebar/border)
                // is what keeps the window fully borderless-looking while still being
                // a "real" WS_CAPTION/WS_THICKFRAME window underneath.
                return;

            case WM_NCHITTEST:
                nint hit = HitTest(m.LParam);

                if (hit != 0)
                {
                    m.Result = hit;
                    return;
                }

                break;
        }

        base.WndProc(ref m);
    }

    private nint HitTest(nint lParam)
    {
        // Snap and edge-resize only make sense while the window is in its normal state -
        // a maximized window has nowhere further to snap to, and resize handles on a
        // maximized window would be meaningless since its bounds are locked to the
        // working area.
        if (WindowState != FormWindowState.Normal)
        {
            return 0;
        }

        Point screenPoint = new(unchecked((short)(long)lParam), unchecked((short)((long)lParam >> 16)));
        Point clientPoint = PointToClient(screenPoint);

        int x = clientPoint.X;
        int y = clientPoint.Y;
        int width = ClientSize.Width;
        int height = ClientSize.Height;

        bool onLeft = x < ResizeBorderThickness;
        bool onRight = x >= width - ResizeBorderThickness;
        bool onTop = y < ResizeBorderThickness;
        bool onBottom = y >= height - ResizeBorderThickness;

        // Corners and edges take priority over caption, since the resize border and the
        // custom titlebar overlap slightly in the top corners.
        if (onTop && onLeft) return HTTOPLEFT;
        if (onTop && onRight) return HTTOPRIGHT;
        if (onBottom && onLeft) return HTBOTTOMLEFT;
        if (onBottom && onRight) return HTBOTTOMRIGHT;
        if (onLeft) return HTLEFT;
        if (onRight) return HTRIGHT;
        if (onTop) return HTTOP;
        if (onBottom) return HTBOTTOM;

        if (_frame.IsCaptionHit(clientPoint))
        {
            return HTCAPTION;
        }

        return HTCLIENT;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
        }

        if (e.KeyCode == Keys.Escape && _isFullscreen)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _previousWindowState = WindowState;
            _previousBounds = Bounds;

            WindowState = FormWindowState.Normal;
            Bounds = Screen.FromControl(this).WorkingArea;

            _isFullscreen = true;

            _shell?.SetStatus(
                "● Creator Workspace fullscreen  •  Press Esc or F11 to exit",
                WadevoTheme.Colors.Success);

            return;
        }

        WindowState = _previousWindowState;
        Bounds = _previousBounds;

        _isFullscreen = false;

        _shell?.SetStatus(
            "● Creator Workspace windowed",
            WadevoTheme.Colors.Success);
    }

    private async void BeginStartup()
    {
        _splash.SetStatus("Loading Wadevo Core...");
        await Task.Delay(250);

        _splash.SetStatus("Starting Overlay Engine...");
        StartOverlayEngine();
        await Task.Delay(250);

        _splash.SetStatus("Connecting to Blaze...");
        StartBlazeLiveEvents();
        await Task.Delay(250);

        // Deliberately independent of Blaze's own try/catch above - these three were
        // previously bundled inside StartBlazeLiveEvents(), meaning any exception from
        // Blaze's own initialization (e.g. a connection hiccup) silently prevented the
        // emote cache and command timer from ever running too, for the entire session,
        // with no visible error and no logical connection to Blaze at all.
        StartIndependentBackgroundServices();
        await Task.Delay(250);

        _splash.SetStatus("Connecting to Serato...");
        StartSongReader();
        WadevoChannelStatsCache.EnsureStarted();
        await Task.Delay(250);

        _splash.SetStatus("Opening creator workspace...");
        await Task.Delay(250);

        ShowShell();
    }

    private void StartIndependentBackgroundServices()
    {
        try
        {
            // Same trick as BlazeLiveEventService.Shared: starts the Timer-mode command
            // scheduler without requiring the Commands page to be opened first. This also
            // covers what the old Vote Reminder toggle used to do - anyone who wants that
            // behavior back can recreate it as a Timer command with a chat-message response.
            _ = TimedCommandService.Shared;
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Timed command service failed to initialize", ex);
        }

        try
        {
            // Background-loads the emote list (BTTV + custom) so it's ready by the time
            // any overlay first renders, rather than the first render happening before
            // the network fetch completes and showing no emotes at all.
            _ = EmoteCache.RefreshAsync();
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Emote cache failed to initialize", ex);
        }
    }

    private void StartBlazeLiveEvents()
    {
        try
        {
            // Just accessing .Shared is enough to create it (it's a lazy singleton) and run
            // its auto-connect logic if Blaze was already authenticated in a previous session.
            // Without this, commands would silently never trigger unless the Blaze page in the
            // sidebar happened to be opened at least once during the session.
            _ = BlazeLiveEventService.Shared;

            WadevoLogger.Info("Blaze live event service initialized.");
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Blaze live event service failed to initialize", ex);
        }
    }

    private void ShowShell()
    {
        _frame.ContentHost.Controls.Clear();

        _shell = new WadevoShell();
        _frame.ContentHost.Controls.Add(_shell);
    }

    private void StartOverlayEngine()
    {
        try
        {
            _overlayServer = new OverlayServer("http://localhost:5050/", () => _currentSong, () => _currentMetadata);
            _overlayServer.Start();

            WadevoLiveStatus.IsOverlayEngineRunning = true;

            _shell?.SetStatus(
                "● Overlay Engine Online  •  localhost:5050",
                WadevoTheme.Colors.Success);
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Overlay Engine failed to start", ex);

            _shell?.SetStatus(
                $"● Overlay Engine Error: {ex.Message}",
                WadevoTheme.Colors.Error);
        }
    }

    private void StartSongReader()
    {
        _songReaderCancellation?.Cancel();

        WadevoAppSettingsModel appSettings = new WadevoAppSettingsStore().Load();
        ApplyDjSoftwareSelection(appSettings.DjSoftware);

        _songReaderCancellation = new CancellationTokenSource();

        bool useVirtualDj = _virtualDjReader is not null;

        _shell?.SetStatus(
            useVirtualDj
                ? "● Overlay Engine Online  •  Waiting for VirtualDJ..."
                : "● Overlay Engine Online  •  Waiting for Serato...",
            WadevoTheme.Colors.Warning);

        _ = Task.Run(() => SongReaderLoop(RefreshSeconds, _songReaderCancellation.Token));
    }

    private void ApplyDjSoftwareSelection(string djSoftware)
    {
        WadevoAppSettingsModel appSettings = new WadevoAppSettingsStore().Load();
        bool useVirtualDj = djSoftware.Equals("VirtualDJ", StringComparison.OrdinalIgnoreCase);
        bool useSeratoLocal = !useVirtualDj && appSettings.SeratoReadMethod.Equals("LocalHistoryFile", StringComparison.OrdinalIgnoreCase);
        bool useSeratoUrl = !useVirtualDj && !useSeratoLocal;

        _seratoService = useSeratoUrl ? new SeratoService(appSettings.SeratoPlaylistUrl) : null;
        _seratoLocalReader = useSeratoLocal ? new SeratoHistoryReader() : null;
        _virtualDjReader = useVirtualDj ? new VirtualDjNowPlayingReader() : null;

        WadevoLiveStatus.IsSeratoConnected = false;
        WadevoLiveStatus.IsVirtualDjConnected = false;
    }

    private async Task<string?> ReadCurrentSongFromActiveSourceAsync()
    {
        if (_virtualDjReader is not null)
        {
            return await _virtualDjReader.GetCurrentSongAsync();
        }

        if (_seratoLocalReader is not null)
        {
            return await _seratoLocalReader.GetCurrentSongAsync();
        }

        if (_seratoService is not null)
        {
            return await _seratoService.GetCurrentSongAsync();
        }

        return null;
    }

    private async Task SongReaderLoop(int refreshSeconds, CancellationToken cancellationToken)
    {
        WadevoAppSettingsModel initialSettings = new WadevoAppSettingsStore().Load();
        string currentDjSoftware = initialSettings.DjSoftware;
        string currentSeratoReadMethod = initialSettings.SeratoReadMethod;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Re-checked every poll (every few seconds) rather than only once at startup,
            // so toggling either setting takes effect live - switching readers on the fly
            // instead of requiring a full app restart.
            WadevoAppSettingsModel latestSettings = new WadevoAppSettingsStore().Load();

            bool djSoftwareChanged = !latestSettings.DjSoftware.Equals(currentDjSoftware, StringComparison.OrdinalIgnoreCase);
            bool seratoMethodChanged = !latestSettings.SeratoReadMethod.Equals(currentSeratoReadMethod, StringComparison.OrdinalIgnoreCase);

            if (djSoftwareChanged || seratoMethodChanged)
            {
                currentDjSoftware = latestSettings.DjSoftware;
                currentSeratoReadMethod = latestSettings.SeratoReadMethod;
                ApplyDjSoftwareSelection(currentDjSoftware);
                _lastSong = null;
            }

            bool usingVirtualDj = _virtualDjReader is not null;
            bool usingSeratoLocal = _seratoLocalReader is not null;

            string sourceName = usingVirtualDj
                ? "VirtualDJ"
                : usingSeratoLocal
                    ? "Serato (Local)"
                    : "Serato";

            try
            {
                if (_seratoService is null && _seratoLocalReader is null && _virtualDjReader is null)
                    return;

                string? song = await ReadCurrentSongFromActiveSourceAsync();

                if (!string.IsNullOrWhiteSpace(song) && song != _lastSong)
                {
                    _lastSong = song;
                    _currentSong = song;

                    if (usingVirtualDj)
                    {
                        WadevoLiveStatus.IsVirtualDjConnected = true;
                    }
                    else
                    {
                        WadevoLiveStatus.IsSeratoConnected = true;
                    }

                    WadevoLiveStatus.CurrentSong = song;

                    SongParts parts = SplitSong(song);

                    BeginInvoke(() =>
                    {
                        _shell?.SetNowPlaying(parts.Artist, parts.Title);

                        _shell?.SetStatus(
                            $"● Overlay Engine Online  •  {sourceName} Connected",
                            WadevoTheme.Colors.Success);
                    });

                    _currentMetadata = null;
                    _ = RefreshMetadataAsync(parts.Artist, parts.Title, song);
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (usingVirtualDj)
                {
                    WadevoLiveStatus.IsVirtualDjConnected = false;
                }
                else
                {
                    WadevoLiveStatus.IsSeratoConnected = false;
                }

                WadevoLogger.Warning($"{sourceName} read failed: {ex.Message}");

                BeginInvoke(() =>
                {
                    _shell?.SetStatus(
                        $"● {sourceName} Error: {ex.Message}",
                        WadevoTheme.Colors.Error);
                });
            }

            await Task.Delay(refreshSeconds * 1000, cancellationToken);
        }
    }

    private async Task RefreshMetadataAsync(string artist, string title, string songAtRequestTime)
    {
        try
        {
            MusicMetadataModel? metadata = await MusicMetadataService.LookupAsync(artist, title);

            // If the song has already changed again while this lookup was in flight, discard the stale result.
            if (metadata is not null && songAtRequestTime == _currentSong)
            {
                _currentMetadata = metadata;
            }
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Music metadata lookup failed: {ex.Message}");
            // A failed lookup just means the overlay falls back to text-only; not worth surfacing to the user.
        }
    }

    private static SongParts SplitSong(string song)
    {
        if (string.IsNullOrWhiteSpace(song))
            return new SongParts("Artist", "Song Title");

        string[] parts = song.Split(" - ", 2, StringSplitOptions.TrimEntries);

        if (parts.Length == 2)
            return new SongParts(parts[0], parts[1]);

        return new SongParts("Song ID", song);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _songReaderCancellation?.Cancel();
        _overlayServer?.Stop();

        base.OnFormClosing(e);
    }

    private record SongParts(string Artist, string Title);
}
