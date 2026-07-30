namespace Wadevo.Modules.OverlayEngine;

using System.Diagnostics;
using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Modules;
using Wadevo.Services;

public class OverlayEngineModule : WadevoModule
{
    private readonly List<OverlayRouteStatus> _routeStatuses = new();
    private readonly System.Windows.Forms.Timer _statusRefreshTimer = new();
    private readonly WadevoScrollablePanel _listPanel = new();

    public OverlayEngineModule()
    {
        BackColor = WadevoTheme.Colors.Background;
        Padding = new Padding(0);
        AutoScroll = false;
        Dock = DockStyle.Fill;

        WadevoGlassCard card = new()
        {
            Dock = DockStyle.Fill,
            AccentColor = WadevoTheme.Colors.Cyan
        };

        Label title = new()
        {
            Text = "🚀 Overlay Engine",
            Location = new Point(28, 24),
            Size = new Size(500, 34),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        Label subtitle = new()
        {
            Text = "Copy overlay URLs or test live events.",
            Location = new Point(30, 62),
            Size = new Size(720, 28),
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label perOverlayNote = new()
        {
            Text = "This page covers Wadevo's core routes. Each individual overlay you build in " +
                   "Overlay Designer (Song ID, Alert layouts, Custom overlays, ...) has its own " +
                   "dedicated OBS URL now too - open any saved overlay there and use its own " +
                   "\"Copy OBS URL\" button, so several can be added as separate Browser Sources " +
                   "at once.",
            Location = new Point(30, 88),
            Size = new Size(720, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        card.Controls.Add(title);
        card.Controls.Add(subtitle);
        card.Controls.Add(perOverlayNote);

        _listPanel.Location = new Point(28, 118);
        _listPanel.Size = new Size(820, 400);
        _listPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _listPanel.BackColor = Color.Transparent;
        _listPanel.Content.Padding = new Padding(0, 0, 0, 12);

        card.Resize += (_, _) =>
        {
            int bottomPadding = 28;
            int availableHeight = card.ClientSize.Height - _listPanel.Top - bottomPadding;

            _listPanel.Height = Math.Max(120, availableHeight);
            _listPanel.Width = Math.Max(320, card.ClientSize.Width - 56);

            int itemWidth = Math.Max(280, _listPanel.ClientSize.Width - 20);

            foreach (Control control in _listPanel.Content.Controls)
            {
                control.Width = itemWidth;
            }
        };

        card.Controls.Add(_listPanel);

        AddOverlayRow("🎵", "Song ID", "Live song overlay", "/nowplaying", OverlayServer.NowPlayingUrl, WadevoTheme.Colors.Cyan);
        AddOverlayRow("💬", "Commands", "Command overlay route", "/commands", OverlayServer.CommandsOverlayUrl, WadevoTheme.Colors.Accent);
        AddOverlayRow("🚨", "Alerts", "Alert overlay route", "/alerts", OverlayServer.AlertsOverlayUrl, WadevoTheme.Colors.Purple);
        AddOverlayRow("🎬", "GIFs", "GIF overlay route", "/gifs", OverlayServer.GifsOverlayUrl, WadevoTheme.Colors.Pink);
        AddOverlayRow("🗳️", "Votes", "Running vote tally for the session", "/votes", OverlayServer.VotesOverlayUrl, WadevoTheme.Colors.Warning);
        AddOverlayRow("💬", "Combined Chat", "Chat from every connected platform, in one feed", "/chat", OverlayServer.ChatOverlayUrl, WadevoTheme.Colors.Cyan);
        AddOverlayRow("🛠", "Debug", "Overlay route status", "/debug", OverlayServer.DebugUrl, WadevoTheme.Colors.Warning);

        WadevoButton configureChatButton = new()
        {
            ButtonText = "⚙ Configure Combined Chat Overlay",
            Size = new Size(300, 40),
            AccentColor = WadevoTheme.Colors.Cyan,
            Margin = new Padding(0, 4, 0, 10)
        };
        configureChatButton.ButtonClicked += (_, _) =>
        {
            using ChatOverlaySettingsForm form = new();
            form.ShowDialog(FindForm());
        };
        _listPanel.Content.Controls.Add(configureChatButton);

        Controls.Add(card);

        OverlayServer.RouteStatusChanged += OverlayServer_RouteStatusChanged;

        _statusRefreshTimer.Interval = 1000;
        _statusRefreshTimer.Tick += (_, _) => UpdateRouteStatuses();
        _statusRefreshTimer.Start();

        Disposed += OverlayEngineModule_Disposed;

        UpdateRouteStatuses();
    }

    private void AddOverlayRow(
        string icon,
        string name,
        string description,
        string route,
        string url,
        Color accentColor)
    {
        WadevoGlassCard row = new()
        {
            Size = new Size(800, 58),
            AccentColor = accentColor,
            ShowGlow = false,
            Padding = new Padding(0),
            BackColor = WadevoTheme.Colors.Card,
            Margin = new Padding(0, 0, 0, 10)
        };

        Label iconLabel = new()
        {
            Text = icon,
            Location = new Point(16, 10),
            Size = new Size(38, 38),
            Font = new Font("Segoe UI Emoji", 18F, FontStyle.Regular),
            ForeColor = accentColor,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label nameLabel = new()
        {
            Text = name,
            Location = new Point(66, 7),
            Size = new Size(150, 20),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label statusLabel = new()
        {
            Text = "⚪ Idle",
            Location = new Point(220, 7),
            Size = new Size(100, 20),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label descriptionLabel = new()
        {
            Text = description,
            Location = new Point(66, 29),
            Size = new Size(170, 20),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label urlLabel = new()
        {
            Text = url,
            Location = new Point(245, 29),
            Size = new Size(225, 20),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoButton copyButton = CreateRowButton("Copy", accentColor, new Point(490, 10));
        copyButton.ButtonClicked += (_, _) =>
        {
            Clipboard.SetText(url);
            WadevoMessageBox.Show(
                FindForm(),
                $"{name} URL copied.\n\nPaste it into OBS as a Browser Source.",
                "Wadevo Overlay Engine");
        };

        WadevoButton testButton = CreateRowButton("Test", accentColor, new Point(584, 10));
        testButton.ButtonClicked += (_, _) => TestOverlay(name, route, url, accentColor);

        WadevoButton openButton = CreateRowButton("Open", accentColor, new Point(678, 10));
        openButton.ButtonClicked += (_, _) => OpenUrl(url);

        row.Controls.Add(iconLabel);
        row.Controls.Add(nameLabel);
        row.Controls.Add(statusLabel);
        row.Controls.Add(descriptionLabel);
        row.Controls.Add(urlLabel);
        row.Controls.Add(copyButton);
        row.Controls.Add(testButton);
        row.Controls.Add(openButton);

        _listPanel.Content.Controls.Add(row);

        _routeStatuses.Add(new OverlayRouteStatus(route, statusLabel));
    }

    private static WadevoButton CreateRowButton(string text, Color accentColor, Point location)
    {
        return new WadevoButton()
        {
            ButtonText = text,
            Location = location,
            Size = new Size(78, 38),
            AccentColor = accentColor,
            BackColor = WadevoTheme.Colors.Card
        };
    }

    private void OverlayServer_RouteStatusChanged()
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(UpdateRouteStatuses);
            return;
        }

        UpdateRouteStatuses();
    }

    private void UpdateRouteStatuses()
    {
        foreach (OverlayRouteStatus routeStatus in _routeStatuses)
        {
            bool isActive = OverlayServer.IsRouteActive(routeStatus.Route);

            routeStatus.StatusLabel.Text = isActive ? "🟢 Active" : "⚪ Idle";
            routeStatus.StatusLabel.ForeColor = isActive
                ? WadevoTheme.Colors.Success
                : WadevoTheme.Colors.TextMuted;
        }
    }

    private void OverlayEngineModule_Disposed(object? sender, EventArgs e)
    {
        OverlayServer.RouteStatusChanged -= OverlayServer_RouteStatusChanged;

        _statusRefreshTimer.Stop();
        _statusRefreshTimer.Dispose();
    }

    private static void TestOverlay(string name, string route, string url, Color accentColor)
    {
        if (route == "/alerts")
        {
            // Alerts now render from whichever specific alert profile fired - there's no
            // longer a single generic "alert" to test from here, since appearance is a
            // per-profile Designer layout. Testing a specific alert (with its own real
            // design) already works from its own Test button in the Alerts tab.
            WadevoMessageBox.Show(
                null,
                "Alerts each have their own look now, designed individually in the Overlay " +
                "Designer. Open a specific alert in the Alerts tab and use its own Test " +
                "button there to preview it with its real design - there's no single generic " +
                "alert to test from here anymore.",
                "Wadevo Overlay Engine");
            return;
        }

        ShowPreviewWindow(name, route, url, accentColor);
    }

    private static void ShowPreviewWindow(string name, string route, string url, Color accentColor)
    {
        Form previewForm = new()
        {
            Text = $"Wadevo Overlay Test - {name}",
            Size = new Size(900, 520),
            StartPosition = FormStartPosition.CenterScreen,
            BackColor = WadevoTheme.Colors.Background,
            MinimumSize = new Size(720, 360)
        };

        Panel topBar = new()
        {
            Dock = DockStyle.Top,
            Height = 54,
            BackColor = WadevoTheme.Colors.Panel
        };

        Label titleLabel = new()
        {
            Text = $"Testing: {name}",
            Location = new Point(18, 9),
            Size = new Size(360, 22),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = accentColor,
            BackColor = Color.Transparent
        };

        Label urlLabel = new()
        {
            Text = url,
            Location = new Point(18, 30),
            Size = new Size(590, 18),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoButton fireButton = new()
        {
            ButtonText = "Fire",
            Location = new Point(620, 8),
            Size = new Size(76, 38),
            AccentColor = accentColor,
            BackColor = WadevoTheme.Colors.Panel,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        WadevoButton refreshButton = new()
        {
            ButtonText = "Refresh",
            Location = new Point(704, 8),
            Size = new Size(86, 38),
            AccentColor = accentColor,
            BackColor = WadevoTheme.Colors.Panel,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        WadevoButton openButton = new()
        {
            ButtonText = "Open",
            Location = new Point(798, 8),
            Size = new Size(74, 38),
            AccentColor = accentColor,
            BackColor = WadevoTheme.Colors.Panel,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        WebBrowser browser = new()
        {
            Dock = DockStyle.Fill,
            ScriptErrorsSuppressed = true
        };

        fireButton.ButtonClicked += (_, _) => FireTestEvent(route);
        refreshButton.ButtonClicked += (_, _) => browser.Navigate(url);
        openButton.ButtonClicked += (_, _) => OpenUrl(url);

        topBar.Controls.Add(titleLabel);
        topBar.Controls.Add(urlLabel);
        topBar.Controls.Add(fireButton);
        topBar.Controls.Add(refreshButton);
        topBar.Controls.Add(openButton);

        previewForm.Controls.Add(browser);
        previewForm.Controls.Add(topBar);

        previewForm.Shown += (_, _) =>
        {
            browser.Navigate(url);

            System.Windows.Forms.Timer delayedFireTimer = new()
            {
                Interval = 1200
            };

            delayedFireTimer.Tick += (_, _) =>
            {
                delayedFireTimer.Stop();
                delayedFireTimer.Dispose();

                FireTestEvent(route);
            };

            delayedFireTimer.Start();
        };

        previewForm.Show();
    }

    private static void FireTestEvent(string route)
    {
        if (route == "/commands")
        {
            OverlayServer.TriggerTestCommand();
            return;
        }

        if (route == "/gifs")
        {
            OverlayServer.TriggerGif(
                Path.Combine(AppContext.BaseDirectory, "Assets", "Logos", "small wadevo logo.png"),
                5000,
                "Test GIF");
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            WadevoMessageBox.Show(
                null,
                $"Could not open overlay URL.\n\n{ex.Message}",
                "Wadevo Overlay Engine");
        }
    }

    private sealed record OverlayRouteStatus(string Route, Label StatusLabel);
}